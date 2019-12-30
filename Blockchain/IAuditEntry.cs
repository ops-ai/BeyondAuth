using System.Collections.Generic;

namespace Blockchain
{
    public interface IAuditEntry
    {
        /// <summary>
        /// Identifier of subject/principal accessing the resource
        /// </summary>
        string Subject { get; set; }

        /// <summary>
        /// Cross-system correlation id which can be used to cross-reference logs in other systems or trace request
        /// </summary>
        string CorrelationId { get; set; }

        /// <summary>
        /// Date the authorization decision was made. Represented as an integer timestamp, measured in the number of seconds since January 1 1970 UTC
        /// </summary>
        long Timestamp { get; set; }

        /// <summary>
        /// Authorization decision made / event
        /// </summary>
        AuthorizationDecisions AuthorizationDecision { get; set; }

        /// <summary>
        /// Identifier of resolver which made the authorization decision
        /// </summary>
        string Resolver { get; set; }

        /// <summary>
        /// Id/name of policy that dictated this requirement
        /// </summary>
        string Policy { get; set; }

        /// <summary>
        /// Action trying to be performed on the resource. Read/Access in coarse policy cases, or driven by ACL/ACE for fine-grained authorization
        /// </summary>
        string Action { get; set; }

        /// <summary>
        /// Unique Id of resource accessed (Ex: file path, document id, record number, url)
        /// </summary>
        string ResourceId { get; set; }

        /// <summary>
        /// Originating system trying to access the resource
        /// </summary>
        string Originator { get; set; }

        /// <summary>
        /// Extra information included by the authorization resolver
        /// </summary>
        IDictionary<string, string> Data { get; set; }

        /// <summary>
        /// Compute hash of audit entry
        /// </summary>
        /// <returns></returns>
        string CalculateAuditEntryHash();
    }
}
