using System;

namespace Identity.Core
{
    internal class Otac
    {
        public string Id { get; set; }

        public string UserId { get; set; }

        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    }
}
