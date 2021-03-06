﻿using NodaTime;
using System;

namespace Presentation.Web.Server
{
    public class Comment
    {
        public string Author { get; set; } = default!;

        public string Text { get; set; } = default!;

        public Instant CreatedAt { get; set; } = Instant.FromDateTimeUtc(DateTime.UtcNow);
    }
}
