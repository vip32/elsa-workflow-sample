using YesSql.Indexes;

namespace Presentation.Web.Server
{
    public class WorkflowStateIndex : MapIndex
    {
        public string StateId { get; set; } = default!;
    }
}