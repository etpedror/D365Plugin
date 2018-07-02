using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace ServiceBusWorkflowActivity
{
    public class SendToServiceBus : CodeActivity
    {
        /// <summary>
        /// This method is called when the workflow executes.
        /// </summary>
        /// <param name="executionContext">The data for the event triggering the workflow.</param>
        protected override void Execute(CodeActivityContext executionContext)
        {
            var context = executionContext.GetExtension<IWorkflowContext>();
            var endpointService = executionContext.GetExtension<IServiceEndpointNotificationService>();
            endpointService.Execute(ServiceEndpoint.Get(executionContext), context);
        }

        /// <summary>
        /// Enables the service endpoint to be provided when this activity is added as a 
        /// step in a workflow.
        /// </summary>
        [RequiredArgument]
        [ReferenceTarget("serviceendpoint")]
        [Input("Input id")]
        public InArgument<EntityReference> ServiceEndpoint { get; set; }
    }
}
