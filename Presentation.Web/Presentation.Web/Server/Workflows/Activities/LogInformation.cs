using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;

namespace Presentation.Web.Server
{
    /// <summary>
    /// Writes a text string to the console.
    /// </summary>
    [Action(
        Category = "Logger",
        Description = "Write message to the logger.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class LogInformation : Activity
    {
        private readonly ILogger<LogInformation> logger;

        public LogInformation(ILogger<LogInformation> logger)
        {
            this.logger = logger;
        }


        [ActivityProperty(Hint = "The message to log.")]
        public string Message { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            using (this.logger.BeginScope(new Dictionary<string, object>
            {
                //["WorkflowCorrelationId"] = context.CorrelationId,
                //["WorkflowDefinitionId"] = context.WorkflowInstance.DefinitionId
                ["WorkflowInstanceId"] = context.WorkflowInstance.Id
            }))
            {
                this.logger.LogInformation("[{WorkflowDefinitionId}::{WorkflowCorrelationId}] " + this.Message, context.WorkflowInstance.DefinitionId, context.CorrelationId);
            }

            return this.Done();
        }
    }
}