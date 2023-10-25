using System;

namespace Identity.Core
{
    public class PasswordResetRequest
    {
        public string Id { get; set; }

        public string UserId { get; set; }

        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

        public bool Handled { get; set; } = false;

        public string? ClientId { get; set; }
    }
}
