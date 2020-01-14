using Cryptography;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Blockchain
{
    [Serializable]
    public class AuditEntry : IAuditEntry
    {
        /// <summary>
        /// Identifier of subject/principal accessing the resource
        /// </summary>
        [JsonProperty("sub")]
        public string Subject { get; set; }

        /// <summary>
        /// Cross-system correlation id which can be used to cross-reference logs in other systems or trace request
        /// </summary>
        [JsonProperty("correlation_id")]
        public string CorrelationId { get; set; }

        /// <summary>
        /// Identifier of system that made the decision
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Date the authorization decision was made. Represented as an integer timestamp, measured in the number of seconds since January 1 1970 UTC
        /// </summary>
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        /// <summary>
        /// Authorization decision made / event
        /// </summary>
        [JsonProperty("decision")]
        public AuthorizationDecisions AuthorizationDecision { get; set; }

        /// <summary>
        /// Identifier of resolver which made the authorization decision
        /// </summary>
        [JsonProperty("resolver")]
        public string Resolver { get; set; }

        /// <summary>
        /// Id/name of policy that dictated this requirement
        /// </summary>
        [JsonProperty("policy")]
        public string Policy { get; set; }

        /// <summary>
        /// Action trying to be performed on the resource. Read/Access in coarse policy cases, or driven by ACL/ACE for fine-grained authorization
        /// </summary>
        [JsonProperty("action")]
        public string Action { get; set; }

        /// <summary>
        /// Unique Id of resource accessed (Ex: file path, document id, record number, url)
        /// </summary>
        [JsonProperty("resource_id")]
        public string ResourceId { get; set; }

        /// <summary>
        /// Originating system trying to access the resource
        /// </summary>
        [JsonProperty("originator")]
        public string Originator { get; set; }

        /// <summary>
        /// Extra information included by the authorization resolver
        /// </summary>
        [JsonProperty("data")]
        public IDictionary<string, string> Data { get; set; } = new Dictionary<string, string>();


        /// <inheritdoc />
        public string CalculateAuditEntryHash()
        {
            var auditHash = $"{Subject}{CorrelationId}{Timestamp}{AuthorizationDecision}{ClientId}{Resolver}{Policy}{Action}{ResourceId}{Originator}{string.Join("", Data.Keys)}{string.Join("", Data.Values)}";
            return Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes(auditHash)));
        }
    }
}
