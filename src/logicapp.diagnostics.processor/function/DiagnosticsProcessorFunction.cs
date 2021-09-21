using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.Security.Core;
using GuardNet;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace logicapp.diagnostics.processor
{
    public class DiagnosticsProcessorFunction : EventHubBasedAzureFunction
    {
        private readonly ISecretProvider _secretProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticsProcessorFunction"/> class.
        /// </summary>
        /// <param name="secretProvider">The instance that provides secrets to the HTTP trigger.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages while handling the HTTP request.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secretProvider"/> is <c>null</c>.</exception>
        public DiagnosticsProcessorFunction(ISecretProvider secretProvider, ILogger<DiagnosticsProcessorFunction> logger) : base(logger)
        {
            Guard.NotNull(secretProvider, nameof(secretProvider), "Requires a secret provider instance");
            _secretProvider = secretProvider;
        }

        [FunctionName("DiagnosticsProcessorFunction")]
        public void Run([EventHubTrigger("logicapp", Connection = "EventHubConnectionString")] string message)
        {
            dynamic data = JObject.Parse(message);
            var records = JArray.Parse(data.records.ToString());

            foreach (JObject record in records)
            {
                var telemetryContext = new Dictionary<string, object>();

                try
                {
                    JToken properties = record.SelectToken("properties");
                    if (properties != null)
                    {
                        // Retrieve runId and workflowName properties and add them to the telemetryContext
                        string runId = properties.SelectToken("resource").Value<string>("runId");
                        string workflowName = properties.SelectToken("resource").Value<string>("workflowName");
                        telemetryContext.Add("RunId", runId);
                        telemetryContext.Add("WorkflowName", workflowName);
                        telemetryContext.Add("OperationId", runId);

                        // Retrieve tracked properties and properties to determine if an error occurred
                        string status = properties.Value<string>("status");
                        string code = properties.Value<string>("code");
                        JToken trackedProperties = properties.SelectToken("trackedProperties");
                        JToken errorProperties = properties.SelectToken("error");

                        // Add tracked properties to the telemetryContext
                        bool containsTrackedProperties = false;
                        if (trackedProperties != null && trackedProperties.Count() > 0)
                        {
                            containsTrackedProperties = true;
                            foreach (JProperty property in trackedProperties)
                            {
                                if (telemetryContext.ContainsKey(property.Name))
                                {
                                    telemetryContext[property.Name] = trackedProperties.Value<string>(property.Name);
                                }
                                else
                                {
                                    telemetryContext.Add(property.Name, trackedProperties.Value<string>(property.Name));
                                }
                            }
                        }

                        // If an error occurred, add the error information to the telemetryContext and log to Application Insights
                        if (status.ToLower() == "failed" && code.ToLower() == "terminated" && errorProperties != null && errorProperties.Count() > 0)
                        {
                            telemetryContext.Add("ErrorCode", errorProperties.Value<string>("code"));
                            telemetryContext.Add("ErrorMessage", errorProperties.Value<string>("message"));

                            Exception ex = new Exception(telemetryContext["ErrorMessage"].ToString());
                            Logger.LogError(ex, telemetryContext["WorkflowName"] + " - Failed. " + telemetryContext["ErrorMessage"]);
                            Logger.LogEvent(telemetryContext["WorkflowName"] + " - Failed. " + telemetryContext["ErrorMessage"], telemetryContext);
                            Logger.LogMetric(telemetryContext["WorkflowName"] + " - Failed", 1, telemetryContext);
                        }
                        else if (containsTrackedProperties)
                        {
                            // Ignore the event if the tracked properties are not yet set to their value
                            if (!telemetryContext.Any(x => x.Value.ToString().Contains("[parameters('")))
                            {
                                // Log the event to Application Insights
                                if (telemetryContext.ContainsKey("EventText"))
                                {
                                    Logger.LogEvent(telemetryContext["WorkflowName"] + ": " + telemetryContext["EventText"], telemetryContext);
                                }
                                else
                                {
                                    Logger.LogEvent(telemetryContext["WorkflowName"].ToString(), telemetryContext);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Exception processing diagnostics. " + ex.ToString());
                }
            }
        }
    }
}