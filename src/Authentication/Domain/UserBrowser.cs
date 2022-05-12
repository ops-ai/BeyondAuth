namespace Authentication.Domain
{
    public class UserBrowser
    {
        /// <summary>
        /// Browser fingerprint
        /// </summary>
        public string Id { get; set; }

        public Dictionary<string, DateTime> UserIds { get; set; } = new Dictionary<string, DateTime>();

        public string UserAgent { get; set; }

        public List<string> IPAddresses { get; set; }

        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

        public DateTime LastSeenOnUtc { get; set; } = DateTime.UtcNow;
    }
}
