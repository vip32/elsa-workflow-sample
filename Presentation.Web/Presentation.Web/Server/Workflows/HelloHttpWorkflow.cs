using System.Net;
using Elsa.Activities.Console;
using Elsa.Activities.Http;
using Elsa.Builders;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;

namespace Presentation.Web.Server
{
    /// <summary>
    /// A workflow that is triggered when HTTP requests are made to /hello and writes a response.
    /// </summary>
    public class HelloHttpWorkflow : IWorkflow
    {
        private readonly ILogger<HelloHttpWorkflow> logger;

        public HelloHttpWorkflow(ILogger<HelloHttpWorkflow> logger)
        {
            this.logger = logger;
        }

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .HttpEndpoint("/_workflows/hello")
                .WriteLine(context =>
                    $"Hello from Elsa! (correlationId={this.GetCorrelationId(context)})")
                .WriteHttpResponse(HttpStatusCode.OK, $"Hello from Elsa!", "text/html");
        }

        private string GetCorrelationId(ActivityExecutionContext context) => context.WorkflowExecutionContext.CorrelationId;
    }
}