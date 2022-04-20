using System.Text.Json.Serialization;

namespace Authentication.Models
{
    public class MailgunWebhookModel
    {
        public SignatureData Signature { get; set; }

        [JsonPropertyName("event-data")]
        public EventData EventData { get; set; }
    }

    public class EventData
    {
        public string Id { get; set; }

        public decimal Timestamp { get; set; }

        [JsonPropertyName("log-level")]
        public string LogLevel { get; set; }

        public string Event { get; set; }

        public string? Ip { get; set; }

        [JsonPropertyName("delivery-status")]
        public DeliveryStatusData? DeliveryStatus { get; set; }

        public FlagData? Flags { get; set; }

        public EnvelopeData? Envelope { get; set; }

        public MessageData Message { get; set; }

        public string? Recipient { get; set; }

        [JsonPropertyName("recipient-domain")]
        public string? RecipientDomain { get; set; }

        public List<string>? Tags { get; set; }

        [JsonPropertyName("user-variables")]
        public Dictionary<string, string>? UserVariables { get; set; }
    }

    public class EnvelopeData
    {
        public string Transport { get; set; }

        public string Sender { get; set; }

        [JsonPropertyName("sending-ip")]
        public string SendingIp { get; set; }

        public string Targets { get; set; }
    }

    public class FlagData
    {
        [JsonPropertyName("is-routed")]
        public bool IsRouted { get; set; }

        [JsonPropertyName("is-authenticated")]
        public bool IsAuthenticated { get; set; }

        [JsonPropertyName("is-system-test")]
        public bool IsSystemTest { get; set; }

        [JsonPropertyName("is-test-mode")]
        public bool IsTestMode { get; set; }
    }

    public class DeliveryStatusData
    {
        public bool Tls { get; set; }

        [JsonPropertyName("mx-host")]
        public string MxHost { get; set; }

        public int Code { get; set; }

        public string Description { get; set; }

        [JsonPropertyName("session-seconds")]
        public double SessionSeconds { get; set; }

        public bool Utf8 { get; set; }

        [JsonPropertyName("attempt-no")]
        public int AttemptNo { get; set; }

        public string Message { get; set; }

        [JsonPropertyName("certificate-validated")]
        public bool CertificateValidated { get; set; }
    }

    public class MessageData
    {
        public MessageHeaderData Headers { get; set; }

        //attachments

        public int Size { get; set; }
    }

    public class MessageHeaderData
    {
        public string To { get; set; }

        [JsonPropertyName("message-id")]
        public string MessageId { get; set; }

        public string From { get; set; }

        public string Subject { get; set; }
    }

    public class SignatureData
    {
        public string Token { get; set; }

        public string Timestamp { get; set; }

        public string Signature { get; set; }
    }
}
