using NodaTime;
using System;
using System.Collections.Generic;

namespace Presentation.Web.Server
{
    public class WorkflowState
    {
        public string Id { get; set; }

        public string CorrelationId { get; set; }

        public Order Order { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        public DemoHttpWorkflowStatus Status = DemoHttpWorkflowStatus.New;

        public Instant CreatedAt { get; set; } = Instant.FromDateTimeUtc(DateTime.UtcNow);
    }
}
