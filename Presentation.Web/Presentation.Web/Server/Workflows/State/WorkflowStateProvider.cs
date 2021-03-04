using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;
using YesSql;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Presentation.Web.Server
{
    public class WorkflowStateProvider : WorkflowContextRefresher<WorkflowState>
    {
        private readonly ISession session;
        private readonly IIdGenerator idGenerator;

        public WorkflowStateProvider(
            ISession session,
            IIdGenerator idGenerator)
        {
            this.session = session;
            this.idGenerator = idGenerator;
        }

        public override async ValueTask<WorkflowState> LoadAsync(
            LoadWorkflowContext context,
            CancellationToken cancellationToken = default) =>
                await this.session.Query<WorkflowState, WorkflowStateIndex>(x => x.StateId == context.ContextId).FirstOrDefaultAsync();

        public override ValueTask<string> SaveAsync(
            SaveWorkflowContext<WorkflowState> context,
            CancellationToken cancellationToken = default)
        {
            var state = context.Context;

            if (string.IsNullOrWhiteSpace(state.Id))
            {
                state.Id = this.idGenerator.Generate();
            }

            this.session.Save(state);
            return new ValueTask<string>(state.Id);
        }
    }
}