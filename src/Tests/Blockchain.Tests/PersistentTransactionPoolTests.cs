using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xunit;

namespace Blockchain.Tests
{
    public class PersistentTransactionPoolTests : IDisposable
    {
        private readonly List<IAuditEntry> _auditEntries;

        public PersistentTransactionPoolTests()
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
            using (var transactionPool = new PersistentTransactionPool())
            {
                transactionPool.AddAuditEntry(_auditEntries[0]);

                var auditEntry = transactionPool.GetAuditEntry();

                Assert.Equal(_auditEntries[0].Subject, auditEntry.Subject);
                Assert.Equal(_auditEntries[0].Timestamp, auditEntry.Timestamp);

                auditEntry = transactionPool.GetAuditEntry();
                Assert.Null(auditEntry);
            }
        }

        [Fact(DisplayName = "Queue with invalid storage location should throw")]
        public void TestInvalidStorage()
        {
            Assert.Throws<InvalidOperationException>(() => new PersistentTransactionPool("x:\\ff", 1000));
            Assert.Throws<ArgumentException>(() => new PersistentTransactionPool("", 1000));
            using (var pool = new PersistentTransactionPool(null, 1000))
                Assert.NotNull(pool);
        }

        [Fact(DisplayName = "Queue with size 1")]
        public void ReallySmallFile()
        {
            using (var pool = new PersistentTransactionPool("./smallfile", 1))
            {
                Assert.NotNull(pool);

                pool.AddAuditEntry(_auditEntries[0]);

                var auditEntry = pool.GetAuditEntry();

                Assert.Equal(_auditEntries[0].Subject, auditEntry.Subject);
                Assert.Equal(_auditEntries[0].Timestamp, auditEntry.Timestamp);
            }
        }

        [Fact(DisplayName = "Empty queue get should return null")]
        public void EmptyQueue()
        {
            using (var transactionPool = new PersistentTransactionPool())
            {
                var entry = transactionPool.GetAuditEntry();
                Assert.Null(entry);
            }
        }

        [Fact(DisplayName = "Constructors")]
        public void TestConstructors()
        {
            using (var pool1 = new PersistentTransactionPool())
            {
                Assert.NotNull(pool1);
                pool1.AddAuditEntry(_auditEntries[0]);

                var auditEntry = pool1.GetAuditEntry();
                Assert.Equal(_auditEntries[0].Subject, auditEntry.Subject);
                Assert.Equal(_auditEntries[0].Timestamp, auditEntry.Timestamp);
            }

            using (var pool2 = new PersistentTransactionPool("./temppool", 1000))
            {
                Assert.NotNull(pool2);
                pool2.AddAuditEntry(_auditEntries[1]);

                var auditEntry = pool2.GetAuditEntry();
                Assert.Equal(_auditEntries[1].Subject, auditEntry.Subject);
                Assert.Equal(_auditEntries[1].Timestamp, auditEntry.Timestamp);
            }

            using (var pool3 = new PersistentTransactionPool("./temppool2"))
            {
                Assert.NotNull(pool3);
                pool3.AddAuditEntry(_auditEntries[2]);

                var auditEntry = pool3.GetAuditEntry();
                Assert.Equal(_auditEntries[2].Subject, auditEntry.Subject);
                Assert.Equal(_auditEntries[2].Timestamp, auditEntry.Timestamp);
            }

            using (var pool4 = new PersistentTransactionPool(new DiskQueue.PersistentQueue("./diskqueue", 1000)))
            {
                Assert.NotNull(pool4);
                pool4.AddAuditEntry(_auditEntries[3]);

                var auditEntry = pool4.GetAuditEntry();
                Assert.Equal(_auditEntries[3].Subject, auditEntry.Subject);
                Assert.Equal(_auditEntries[3].Timestamp, auditEntry.Timestamp);
            }
        }

        [Fact(DisplayName = "Test large dataset")]
        public void LargeSet()
        {
            using (var transactionPool = new PersistentTransactionPool())
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

        [Fact(DisplayName = "If a non running process has a lock then can start an instance")]
        public void NonRunningProcessLock()
        {
            Directory.CreateDirectory("./lockedpool");
            var lockFilePath = Path.Combine("./lockedpool", "lock");
            File.WriteAllText(lockFilePath, "87878768768");

            using (var pool = new PersistentTransactionPool("./lockedpool", 1000))
                Assert.NotNull(pool);
        }

        [Fact(DisplayName = "Can enqueue and dequeue data after restart")]
        public void SurviveRestart()
        {
            using (var queue = new PersistentTransactionPool("./restart", 1000))
            {
                queue.AddAuditEntry(_auditEntries[5]);
            }

            using (var queue = new PersistentTransactionPool("./restart", 1000))
            {
                var auditEntry = queue.GetAuditEntry();

                Assert.Equal(_auditEntries[5].Subject, auditEntry.Subject);
                Assert.Equal(_auditEntries[5].Timestamp, auditEntry.Timestamp);
            }
        }

        [Fact(DisplayName = "Can enqueue and dequeue on separate threads")]
        public void SeparateThreads()
        {
            using (var transactionPool = new PersistentTransactionPool())
            {
                int t1s, t2s;
                t1s = t2s = 0;
                const int target = 100;
                var rnd = new Random();

                var t1 = new Thread(() =>
                {
                    for (int i = 0; i < target; i++)
                    {
                        transactionPool.AddAuditEntry(new AuditEntry());
                        Interlocked.Increment(ref t1s);
                        Thread.Sleep(rnd.Next(0, 100));
                    }
                });
                var t2 = new Thread(() =>
                {
                    for (int i = 0; i < target; i++)
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

        public void Dispose()
        {
            if (Directory.Exists("./transactionpool")) Directory.Delete("./transactionpool", true);
            if (Directory.Exists("./restart")) Directory.Delete("./restart", true);
            if (Directory.Exists("./lockedpool")) Directory.Delete("./lockedpool", true);
            if (Directory.Exists("./smallfile")) Directory.Delete("./smallfile", true);
            if (Directory.Exists("./temppool")) Directory.Delete("./temppool", true);
            if (Directory.Exists("./temppool2")) Directory.Delete("./temppool2", true);
            if (Directory.Exists("./diskqueue")) Directory.Delete("./diskqueue", true);
        }
    }
}
