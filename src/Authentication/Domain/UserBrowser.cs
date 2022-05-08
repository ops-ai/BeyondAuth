namespace Authentication.Domain
{
    public class UserBrowser
    {
        /// <summary>
        /// Browser fingerprint
        /// </summary>
        public string Id { get; set; }

        public List<string> UserIds { get; set; } = new List<string>();

        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

        public DateTime LastSeenOnUtc { get; set; } = DateTime.UtcNow;
    }
}
