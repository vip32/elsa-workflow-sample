using System;
using System.Linq.Expressions;
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
    [Route("_workflows")]
    public class WorkflowController : Controller
    {
        private readonly IWorkflowRunner workflowRunner;
        private readonly IWorkflowInstanceStore instanceStore;

        public WorkflowController(IWorkflowRunner workflowRunner, IWorkflowInstanceStore instanceStore)
        {
            this.workflowRunner = workflowRunner;
            this.instanceStore = instanceStore;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var instances = await this.instanceStore.FindManyAsync(
                new WorkflowInstanceAllSpecification(),
                OrderBySpecification.OrderByDescending<WorkflowInstance>(x => x.CreatedAt));


            return new JsonResult(instances);
        }

        [HttpGet]
        [Route("{correlationId}")]
        public async Task<IActionResult> Get(string correlationId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(correlationId))
            {
                this.Response.StatusCode = StatusCodes.Status400BadRequest;
                return new EmptyResult();
            }

            var instances = await this.instanceStore.FindManyAsync(
                new WorkflowInstanceCorrelationIdSpecification(correlationId),
                OrderBySpecification.OrderByDescending<WorkflowInstance>(x => x.CreatedAt));


            return new JsonResult(instances);
        }
    }

    public class WorkflowInstanceAllSpecification : Specification<WorkflowInstance>
    {
        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => x => x.DefinitionId != null;
    }

    public class WorkflowInstanceCorrelationIdSpecification : Specification<WorkflowInstance>
    {
        public string CorrelationId { get; set; }

        public WorkflowInstanceCorrelationIdSpecification(string correlationId) => this.CorrelationId = correlationId;

        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => x => x.CorrelationId == this.CorrelationId;
    }
}