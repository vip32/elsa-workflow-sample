using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;
using YesSql;
using YesSql.Indexes;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Presentation.Web.Server
{
    public class DemoHttpWorkflowContextProvider : WorkflowContextRefresher<DemoHttpWorkflowContext>
    {
        private readonly ISession _session;
        private readonly IIdGenerator _idGenerator;

        public DemoHttpWorkflowContextProvider(
            ISession session,
            IIdGenerator idGenerator)
        {
            _session = session;
            _idGenerator = idGenerator;
        }

        public override async ValueTask<DemoHttpWorkflowContext?> LoadAsync(
            LoadWorkflowContext context,
            CancellationToken cancellationToken = default) =>
            await _session.Query<DemoHttpWorkflowContext, DemoHttpWorkflowContextIndex>(x => x.Uid == context.ContextId).FirstOrDefaultAsync();

        public override ValueTask<string?> SaveAsync(
            SaveWorkflowContext<DemoHttpWorkflowContext> context,
            CancellationToken cancellationToken = default)
        {
            var workflowContext = context.Context;

            if (string.IsNullOrWhiteSpace(workflowContext.Id))
                workflowContext.Id = _idGenerator.Generate();

            _session.Save(workflowContext);
            return new ValueTask<string?>(workflowContext.Id);
        }
    }

    public class DemoHttpWorkflowContextIndex : MapIndex
    {
        public string Uid { get; set; } = default!; // DocumentId is a reserved column name by YesSql, so taking DocumentUid instead.
    }

    public class DemoHttpWorkflowContextIndexProvider : IndexProvider<DemoHttpWorkflowContext>
    {
        public override void Describe(DescribeContext<DemoHttpWorkflowContext> context)
        {
            context.For<DemoHttpWorkflowContextIndex>().Map(
                x => new DemoHttpWorkflowContextIndex
                {
                    Uid = x.Id
                });
        }
    }
}