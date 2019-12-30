using Cryptography;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Blockchain
{
    public class AuditEntry : IAuditEntry
    {
        /// <inheritdoc />
        [JsonProperty("sub")]
        public string Subject { get; set; }

        /// <inheritdoc />
        [JsonProperty("correlation_id")]
        public string CorrelationId { get; set; }

        /// <inheritdoc />
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        /// <inheritdoc />
        [JsonProperty("decision")]
        public AuthorizationDecisions AuthorizationDecision { get; set; }

        /// <inheritdoc />
        [JsonProperty("resolver")]
        public string Resolver { get; set; }

        /// <inheritdoc />
        [JsonProperty("policy")]
        public string Policy { get; set; }

        /// <inheritdoc />
        [JsonProperty("action")]
        public string Action { get; set; }

        /// <inheritdoc />
        [JsonProperty("resource_id")]
        public string ResourceId { get; set; }

        /// <inheritdoc />
        [JsonProperty("originator")]
        public string Originator { get; set; }

        /// <inheritdoc />
        [JsonProperty("data")]
        public IDictionary<string, string> Data { get; set; } = new Dictionary<string, string>();


        /// <inheritdoc />
        public string CalculateAuditEntryHash()
        {
            string auditHash = $"{Subject}{CorrelationId}{Timestamp}{AuthorizationDecision}{Resolver}{Policy}{Action}{ResourceId}{Originator}{string.Join("", Data.Keys)}{string.Join("", Data.Values)}";
            return Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes(auditHash)));
        }
    }
}
