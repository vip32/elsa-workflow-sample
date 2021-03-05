using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
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
        private readonly IWorkflowInstanceStore instanceStore;

        public DemoHttpWorkflowController(
            IWorkflowRunner workflowRunner, 
            IWorkflowInstanceStore instanceStore)
        {
            this.workflowRunner = workflowRunner;
            this.instanceStore = instanceStore;
        }

        [HttpPost]
        [Route("start")]
        public async Task<IActionResult> Start([FromBody] Order model, CancellationToken cancellationToken = default)
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

            //var results = await this.instanceStore.FindManyAsync(
            //    new WorkflowDefinitionIdSpecification(nameof(DemoHttpWorkflow)),
            //    OrderBySpecification.OrderByDescending<WorkflowInstance>(x => x.CreatedAt),
            //    Paging.Page(1, 2));

            this.Response.StatusCode = StatusCodes.Status202Accepted;
            this.Response.Headers.Add("CorrelationId", instance.CorrelationId);
            return new JsonResult(instance);
        }
    }
}