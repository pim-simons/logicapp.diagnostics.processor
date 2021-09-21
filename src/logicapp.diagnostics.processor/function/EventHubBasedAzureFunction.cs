using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace logicapp.diagnostics.processor
{
    /// <summary>
    /// Represents the base functionality for an Azure Function implementation.
    /// </summary>
    public abstract class EventHubBasedAzureFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubBasedAzureFunction"/> class.
        /// </summary>
        /// <param name="logger">The logger instance to write diagnostic messages throughout the execution of the HTTP trigger.</param>
        protected EventHubBasedAzureFunction(ILogger logger)
        {
            Logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Gets the logger instance used throughout this Azure Function.
        /// </summary>
        protected ILogger Logger { get; }
    }
}