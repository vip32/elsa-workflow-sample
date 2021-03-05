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
    [Route("_workflows/demo")]
    public class DemoHttpWorkflowController : Controller
    {
        private readonly IWorkflowRunner workflowRunner;
        private readonly ISignaler signaler;

        public DemoHttpWorkflowController(
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

            WorkflowInstance instance = await this.workflowRunner.RunWorkflowAsync<DemoHttpWorkflow>(
                input: model,
                correlationId: Guid.NewGuid().ToString("N"),
                cancellationToken: cancellationToken);

            this.Response.StatusCode = StatusCodes.Status202Accepted;
            this.Response.Headers.Add("CorrelationId", instance.CorrelationId);
            return new JsonResult(instance);
        }

        [HttpPost]
        [Route("approve")]
        public async Task<IActionResult> SignalApprove([FromBody] Comment model, [FromQuery] string correlationId, CancellationToken cancellationToken = default)
        {
            if (model == null)
            {
                this.Response.StatusCode = StatusCodes.Status400BadRequest;
                return new EmptyResult();
            }

            await this.signaler.SendSignalAsync("approve", input: model, correlationId: correlationId, cancellationToken: cancellationToken);

            this.Response.StatusCode = StatusCodes.Status202Accepted;
            this.Response.Headers.Add("CorrelationId", correlationId);
            return new EmptyResult();
        }

        [HttpPost]
        [Route("reject")]
        public async Task<IActionResult> SignalReject([FromBody] Comment model, [FromQuery] string correlationId, CancellationToken cancellationToken = default)
        {
            if (model == null)
            {
                this.Response.StatusCode = StatusCodes.Status400BadRequest;
                return new EmptyResult();
            }

            await this.signaler.SendSignalAsync("reject", input: model, correlationId: correlationId, cancellationToken: cancellationToken);

            this.Response.StatusCode = StatusCodes.Status202Accepted;
            this.Response.Headers.Add("CorrelationId", correlationId);
            return new EmptyResult();
        }
    }
}