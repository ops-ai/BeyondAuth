using System;

namespace Blockchain
{
    public interface IAuditEntry
    {
        /// <summary>
        /// Identifier of subject accessing the resource
        /// </summary>
        string Subject { get; set; }

        /// <summary>
        /// Cross-system correlation id which can be used to cross-reference logs in other systems or trace request
        /// </summary>
        string CorrelationId { get; set; }

        /// <summary>
        /// Date the authorization decision was made
        /// </summary>
        DateTime DecisionDate { get; set; }

        /// <summary>
        /// Authorization decision made
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
        /// Unique Id of resource accessed (Ex: file path, document id, record number, url)
        /// </summary>
        string ResourceId { get; set; }

        /// <summary>
        /// Compute hash of audit entry
        /// </summary>
        /// <returns></returns>
        string CalculateAuditEntryHash();
    }
}
