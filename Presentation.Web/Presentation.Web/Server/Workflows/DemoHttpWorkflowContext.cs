using System.Collections.Generic;

namespace Presentation.Web.Server
{
    public class DemoHttpWorkflowContext
    {
        public string Id { get; set; }

        public string CorrelationId { get; set; }

        public Order Order { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
