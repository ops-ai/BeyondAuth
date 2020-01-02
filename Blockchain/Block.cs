using Clifton.Blockchain;
using Cryptography;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blockchain
{
    public class Block : IBlock
    {
        /// <inheritdoc />
        [JsonProperty("entries")]
        public IList<IAuditEntry> AuditEntries { get; } = new List<IAuditEntry>();

        /// <inheritdoc />
        [JsonProperty("idx")]
        public int BlockNumber { get; private set; }

        /// <inheritdoc />
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        /// <inheritdoc />
        [JsonProperty("hash")]
        public string BlockHash { get; private set; }

        /// <inheritdoc />
        [JsonProperty("prevHash")]
        public string PreviousBlockHash { get; set; }

        /// <inheritdoc />
        [JsonProperty("ver")]
        public int Version { get; set; }

        /// <inheritdoc />
        [JsonProperty("sig")]
        public string BlockSignature { get; private set; }

        /// <inheritdoc />
        [JsonProperty("difficulty")]
        public int Difficulty { get; private set; }

        /// <inheritdoc />
        [JsonProperty("nonce")]
        public int Nonce { get; private set; }

        /// <summary>
        /// Merkle tree to aid in validation of audit entries included in block
        /// </summary>
        private MerkleTree merkleTree = new MerkleTree();

        /// <summary>
        /// Create a new block
        /// </summary>
        /// <param name="blockNumber">Index to assign block</param>
        /// <param name="keystore">Key store</param>
        public Block(int blockNumber, int miningDifficulty, IKeyStore keystore)
        {
            BlockNumber = blockNumber;

            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            KeyStore = keystore;
            Difficulty = miningDifficulty;
        }

        /// <inheritdoc />
        public void AddAuditEntry(IAuditEntry auditEntry)
        {
            AuditEntries.Add(auditEntry);
        }

        /// <inheritdoc />
        public string CalculateBlockHash(string previousBlockHash)
        {
            string blockheader = $"{BlockNumber}{Timestamp}{previousBlockHash}";
            string combined = $"{merkleTree.RootNode}{blockheader}";

            string completeBlockHash = Convert.ToBase64String(Hashing.ComputeHmacSha256(Encoding.UTF8.GetBytes(combined), KeyStore.AuthenticatedHashKey));

            return completeBlockHash;
        }

        public string CalculateProofOfWork(string blockHash)
        {
            string difficulty = string.Join("", Enumerable.Range(0, Difficulty).Select(t => '0'));

            while (true)
            {
                string hashedData = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes(Nonce + blockHash)));

                if (hashedData.StartsWith(difficulty, StringComparison.Ordinal))
                    return hashedData;

                Nonce++;
            }
        }

        /// <inheritdoc />
        public void SetBlockHash(IBlock previousBlock)
        {
            if (previousBlock != null)
            {
                PreviousBlockHash = previousBlock.BlockHash;
                previousBlock.NextBlock = this;
            }
            else
            {
                // Previous block is the genesis block.
                PreviousBlockHash = null;
            }

            BuildMerkleTree();

            BlockHash = CalculateProofOfWork(CalculateBlockHash(PreviousBlockHash));

            BlockSignature = KeyStore.SignBlock(BlockHash);
        }

        private void BuildMerkleTree()
        {
            merkleTree = new MerkleTree();

            foreach (var audit in AuditEntries)
                merkleTree.AppendLeaf(MerkleHash.Create(audit.CalculateAuditEntryHash()));

            merkleTree.BuildTree();
        }

        /// <inheritdoc />
        public IBlock NextBlock { get; set; }

        /// <inheritdoc />
        public bool IsValidChain(string prevBlockHash)
        {
            bool isValid = true;

            BuildMerkleTree();

            if (!KeyStore.VerifyBlock(BlockHash, BlockSignature))
                isValid = false;

            // Is this a valid block and transaction
            string newBlockHash = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes(Nonce + CalculateBlockHash(prevBlockHash))));

            if (!KeyStore.VerifyBlock(BlockHash, BlockSignature))
                isValid = false;

            if (!isValid || newBlockHash != BlockHash)
            {
                isValid = false;
            }
            else
            {
                // Does the previous block hash match the latest previous block hash
                isValid |= PreviousBlockHash == prevBlockHash;
            }
            
            // Check the next block by passing in our newly calculated blockhash. This will be compared to the previous
            // hash in the next block. They should match for the chain to be valid.
            // Shortcut validation if current block is not valid
            if (isValid && NextBlock != null)
                return NextBlock.IsValidChain(newBlockHash);

            return isValid;
        }

        /// <inheritdoc />
        public IKeyStore KeyStore { get; }
    }
}
