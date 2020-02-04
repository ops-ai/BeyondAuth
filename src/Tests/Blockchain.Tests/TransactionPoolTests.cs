using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Blockchain.Tests
{
    public class TransactionPoolTests
    {
        private readonly List<IAuditEntry> _auditEntries;

        public TransactionPoolTests()
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
        }

        [Fact(DisplayName = "Queue and dequeue an audit entry")]
        public void TestQueue()
        {
            using (var transactionPool = new TransactionPool())
            {
                transactionPool.AddAuditEntry(_auditEntries[0]);

                var auditEntry = transactionPool.GetAuditEntry();

                Assert.Equal(_auditEntries[0].Subject, auditEntry.Subject);
                Assert.Equal(_auditEntries[0].Timestamp, auditEntry.Timestamp);

                auditEntry = transactionPool.GetAuditEntry();
                Assert.Null(auditEntry);
            }
        }

        [Fact(DisplayName = "Empty queue get should return null")]
        public void EmptyQueue()
        {
            using (var transactionPool = new TransactionPool())
            {
                var entry = transactionPool.GetAuditEntry();
                Assert.Null(entry);
            }
        }

        [Fact(DisplayName = "Test large dataset")]
        public void LargeSet()
        {
            using (var transactionPool = new TransactionPool())
            {
                for (var i = 0; i < 1000; i++)
                    transactionPool.AddAuditEntry(_auditEntries[i % _auditEntries.Count]);

                for (var i = 0; i < 1000; i++)
                {
                    var auditEntry = transactionPool.GetAuditEntry();
                    Assert.NotNull(auditEntry);
                    Assert.Equal(_auditEntries[i % _auditEntries.Count].Subject, auditEntry.Subject);
                    Assert.Equal(_auditEntries[i % _auditEntries.Count].Timestamp, auditEntry.Timestamp);
                }

                var entry = transactionPool.GetAuditEntry();
                Assert.Null(entry);
            }
        }

        [Fact(DisplayName = "Can enqueue and dequeue on separate threads")]
        public void SeparateThreads()
        {
            using (var transactionPool = new TransactionPool())
            {
                int t1s, t2s;
                t1s = t2s = 0;
                const int target = 100;
                var rnd = new Random();

                var t1 = new Thread(() =>
                {
                    for (var i = 0; i < target; i++)
                    {
                        transactionPool.AddAuditEntry(new AuditEntry());
                        Interlocked.Increment(ref t1s);
                        Thread.Sleep(rnd.Next(0, 100));
                    }
                });
                var t2 = new Thread(() =>
                {
                    for (var i = 0; i < target; i++)
                    {
                        transactionPool.GetAuditEntry();
                        Interlocked.Increment(ref t2s);
                        Thread.Sleep(rnd.Next(0, 100));
                    }
                });

                t1.Start();
                t2.Start();

                t1.Join();
                t2.Join();
                Assert.Equal(target, t1s);
                Assert.Equal(target, t2s);
            }
        }
    }
}
