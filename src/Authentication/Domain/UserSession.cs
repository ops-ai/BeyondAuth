namespace Authentication.Domain
{
    public class UserSession
    {
        public string Id { get; set; }

        public string UserId { get; set; }

        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

        public DateTime LastSeenOnUtc { get; set; } = DateTime.UtcNow;

        public List<string> BrowserIds { get; set; }

        public List<string> IPAddresses { get; set; }

        public string UserAgent { get; set; }

        public DateTimeOffset? MaxExpireOnUtc { get; set; }

        public string Idp { get; set; }
        
        public string Amr { get; set; }
    }
}
