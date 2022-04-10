namespace Authentication.Models
{
    public class SmsWebhookModel
    {
        public string AccountSid { get; set; }

        public string From { get; set; }

        public string MessageSid { get; set; }

        public string MessageStatus { get; set; }

        public string SmsSid { get; set; }

        public string SmsStatus { get; set; }
    }
}
