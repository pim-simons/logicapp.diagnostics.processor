# Logic App Diagnostics  Processor
Logic Apps are able to output its diagnostics to an Event Hub, this is an implementation of an Azure Function that processes this diagnostics and send it to Application Insights.

### Configuration
The Azure Function uses an EventHubTrigger where the Event Hub name is called logicapp, the EventHubConnectionString should be configured on the Function Site configuration.

### Functionality
If the diagnostics from the Logic Apps contains tracked properties the tracked properties will be added to the telemetry context and sent to Application Insights as an event. 
The name of the Logic App is used as the name of the event, if a tracked property called ```EventText``` is present this will be used in the name of the event as well. 