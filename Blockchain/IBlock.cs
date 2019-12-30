using System.Collections.Generic;

namespace Blockchain
{
    public interface IBlock
    {
        /// <summary>
        /// List of authorization decisions made
        /// </summary>
        IList<IAuditEntry> AuditEntries { get; }

        // Block header data
        /// <summary>
        /// Block sequence number
        /// </summary>
        int BlockNumber { get; }

        /// <summary>
        /// Date the block was created
        /// </summary>
        long Timestamp { get; set; }

        /// <summary>
        /// Current block hash
        /// </summary>
        string BlockHash { get; }

        /// <summary>
        /// Hash of previous block in chain
        /// </summary>
        string PreviousBlockHash { get; set; }

        /// <summary>
        /// Block structure version to account for future changes
        /// </summary>
        int Version { get; set; }

        /// <summary>
        /// HMAC signature of block
        /// </summary>
        string BlockSignature { get; }


        /// <summary>
        /// Record a new authorization decision
        /// </summary>
        /// <param name="auditEntry"></param>
        void AddAuditEntry(IAuditEntry auditEntry);

        /// <summary>
        /// Compute hash of current block
        /// </summary>
        /// <param name="previousBlockHash">Hash of previous block</param>
        /// <returns></returns>
        string CalculateBlockHash(string previousBlockHash);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="previousBlock"></param>
        void SetBlockHash(IBlock previousBlock);

        /// <summary>
        /// Reference to next block
        /// </summary>
        IBlock NextBlock { get; set; }

        /// <summary>
        /// Check if the previous block's hash is valid
        /// </summary>
        /// <param name="prevBlockHash"></param>
        /// <returns></returns>
        bool IsValidChain(string prevBlockHash);

        /// <summary>
        /// Abstracted key store to allow interop with HSMs and KMS'
        /// </summary>
        IKeyStore KeyStore { get; }
    }
}
