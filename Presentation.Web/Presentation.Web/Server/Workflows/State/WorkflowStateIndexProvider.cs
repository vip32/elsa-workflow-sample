using YesSql.Indexes;

namespace Presentation.Web.Server
{
    public class WorkflowStateIndexProvider : IndexProvider<WorkflowState>
    {
        public override void Describe(DescribeContext<WorkflowState> context)
        {
            context.For<WorkflowStateIndex>().Map(
                x => new WorkflowStateIndex
                {
                    StateId = x.Id
                });
        }
    }
}