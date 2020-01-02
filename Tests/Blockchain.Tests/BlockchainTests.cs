using Cryptography;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Blockchain.Tests
{
    public class BlockchainTests
    {
        private readonly List<IAuditEntry> _auditEntries;

        private readonly TransactionPool _transactionPool = new TransactionPool();

        public BlockchainTests()
        {
            _auditEntries = new List<IAuditEntry>
            {
                new AuditEntry { Action = "access", AuthorizationDecision = AuthorizationDecisions.Granted, Originator = "test", Policy = "policy1", Subject = "testuserid", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                new AuditEntry { Action = "access", AuthorizationDecision = AuthorizationDecisions.Granted, Originator = "test", Policy = "policy1", Subject = "testuserid", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                new AuditEntry { Action = "access", AuthorizationDecision = AuthorizationDecisions.Granted, Originator = "test", Policy = "policy1", Subject = "testuserid5", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                new AuditEntry { Action = "access", AuthorizationDecision = AuthorizationDecisions.Granted, Originator = "test", Policy = "policy2", Subject = "testuserid5", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                new AuditEntry { Action = "access", AuthorizationDecision = AuthorizationDecisions.Granted, Originator = "test", Policy = "policy2", Subject = "testuserid5", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                new AuditEntry { Action = "access", AuthorizationDecision = AuthorizationDecisions.Granted, Originator = "test", Policy = "policy1", Subject = "testuserid5", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                new AuditEntry { Action = "access", AuthorizationDecision = AuthorizationDecisions.Granted, Originator = "test", Policy = "policy1", Subject = "testuserid", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                new AuditEntry { Action = "access", AuthorizationDecision = AuthorizationDecisions.Granted, Originator = "test5", Policy = "policy3", Subject = "testuserid", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                new AuditEntry { Action = "access", AuthorizationDecision = AuthorizationDecisions.Granted, Originator = "test5", Policy = "policy3", Subject = "testuserid8", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                new AuditEntry { Action = "access", AuthorizationDecision = AuthorizationDecisions.Granted, Originator = "test5", Policy = "policy3", Subject = "testuserid8", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                new AuditEntry { Action = "access", AuthorizationDecision = AuthorizationDecisions.Granted, Originator = "test5", Policy = "policy3", Subject = "testuserid8", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                new AuditEntry { Action = "access", AuthorizationDecision = AuthorizationDecisions.Granted, Originator = "test5", Policy = "policy1", Subject = "testuserid8", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                new AuditEntry { Action = "access", AuthorizationDecision = AuthorizationDecisions.Granted, Originator = "test5", Policy = "policy1", Subject = "testuserid9", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                new AuditEntry { Action = "access", AuthorizationDecision = AuthorizationDecisions.Granted, Originator = "test5", Policy = "policy7", Subject = "testuserid9", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                new AuditEntry { Action = "access", AuthorizationDecision = AuthorizationDecisions.Granted, Originator = "test5", Policy = "policy7", Subject = "testuserid9", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                new AuditEntry { Action = "access", AuthorizationDecision = AuthorizationDecisions.Granted, Originator = "test5", Policy = "policy7", Subject = "testuserid9", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
            };

            _auditEntries.ForEach(_transactionPool.AddAuditEntry);
        }

        [Fact]
        public void TestKeyStore()
        {
            var hmacKey = new byte[32];
            using (var randomNumberGenerator = new RNGCryptoServiceProvider())
            {
                randomNumberGenerator.GetBytes(hmacKey);
            }

            IKeyStore keyStore = new KeyStore(hmacKey);

            var blockHash = Convert.ToBase64String(Hashing.ComputeHmacSha256(Encoding.UTF8.GetBytes("test"), keyStore.AuthenticatedHashKey));

            var signedBlock = keyStore.SignBlock(blockHash);

            Assert.NotNull(signedBlock);

            Assert.True(keyStore.VerifyBlock(blockHash, signedBlock));
        }

        [Fact]
        public async Task BlockchainWorkflow()
        {
            var hmacKey = new byte[32];
            using (var randomNumberGenerator = new RNGCryptoServiceProvider())
            {
                randomNumberGenerator.GetBytes(hmacKey);
            }

            IKeyStore keyStore = new KeyStore(hmacKey);
            var audit5 = _auditEntries[4];

            IBlock block1 = new Block(0, 3, keyStore);
            IBlock block2 = new Block(1, 3, keyStore);
            IBlock block3 = new Block(2, 3, keyStore);
            IBlock block4 = new Block(3, 3, keyStore);

            block1.AddAuditEntry(_transactionPool.GetAuditEntry());
            block1.AddAuditEntry(_transactionPool.GetAuditEntry());
            block1.AddAuditEntry(_transactionPool.GetAuditEntry());
            block1.AddAuditEntry(_transactionPool.GetAuditEntry());

            block2.AddAuditEntry(_transactionPool.GetAuditEntry());
            block2.AddAuditEntry(_transactionPool.GetAuditEntry());
            block2.AddAuditEntry(_transactionPool.GetAuditEntry());
            block2.AddAuditEntry(_transactionPool.GetAuditEntry());

            block3.AddAuditEntry(_transactionPool.GetAuditEntry());
            block3.AddAuditEntry(_transactionPool.GetAuditEntry());
            block3.AddAuditEntry(_transactionPool.GetAuditEntry());
            block3.AddAuditEntry(_transactionPool.GetAuditEntry());

            block4.AddAuditEntry(_transactionPool.GetAuditEntry());
            block4.AddAuditEntry(_transactionPool.GetAuditEntry());
            block4.AddAuditEntry(_transactionPool.GetAuditEntry());
            block4.AddAuditEntry(_transactionPool.GetAuditEntry());

            block1.SetBlockHash(null);
            block2.SetBlockHash(block1);
            block3.SetBlockHash(block2);
            block4.SetBlockHash(block3);

            var chain = new BlockChain();
            await chain.AcceptBlock(block1);
            await chain.AcceptBlock(block2);
            await chain.AcceptBlock(block3);
            await chain.AcceptBlock(block4);

            Assert.True(await chain.VerifyChain());

            audit5.Originator = "fakeorig";
            Assert.False(await chain.VerifyChain());
        }
    }
}
