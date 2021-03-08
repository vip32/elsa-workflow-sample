using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Services;
using Elsa.Models;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Web.Server.Controllers
{
    [ApiController]
    [Route("_workflows/simple")]
    public class SimpleWorkflowController : Controller
    {
        private readonly IWorkflowRunner workflowRunner;
        private readonly ISignaler signaler;

        public SimpleWorkflowController(
            IWorkflowRunner workflowRunner,
            ISignaler signaler)
        {
            this.workflowRunner = workflowRunner;
            this.signaler = signaler;
        }

        [HttpPost]
        [Route("start")]
        public async Task<IActionResult> StartWorkflow([FromBody] Order model, CancellationToken cancellationToken = default)
        {
            if(model == null)
            {
                this.Response.StatusCode = StatusCodes.Status400BadRequest;
                return new EmptyResult();
            }

            WorkflowInstance instance = await this.workflowRunner.RunWorkflowAsync<SimpleWorkflow>(
                input: model,
                correlationId: Guid.NewGuid().ToString("N"),
                cancellationToken: cancellationToken);

            this.Response.StatusCode = StatusCodes.Status202Accepted;
            this.Response.Headers.Add("CorrelationId", instance.CorrelationId);
            return new JsonResult(instance);
        }
    }
}