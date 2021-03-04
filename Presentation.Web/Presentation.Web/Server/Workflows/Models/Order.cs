using NodaTime;
using System;

namespace Presentation.Web.Server
{
    public class Order
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public Instant CreatedAt { get; set; } = Instant.FromDateTimeUtc(DateTime.UtcNow);
    }
}
