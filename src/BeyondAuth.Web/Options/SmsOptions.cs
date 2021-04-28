namespace BeyondAuth.Web.Options
{
    /// <summary>
    /// Settings for sending text messages
    /// </summary>
    public class SmsOptions
    {
        /// <summary>
        /// Account SID
        /// </summary>
        public string SmsAccountIdentification { get; set; }

        /// <summary>
        /// Account secret
        /// </summary>
        public string SmsAccountPassword { get; set; }

        /// <summary>
        /// Phone number to send messages from
        /// </summary>
        public string SmsAccountFrom { get; set; }
    }
}
