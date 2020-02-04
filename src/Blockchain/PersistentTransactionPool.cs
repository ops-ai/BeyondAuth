using Blockchain.Extensions;
using DiskQueue;
using System;
using System.Threading;

namespace Blockchain
{
    /// <summary>
    /// A disk-persisted audit entry transaction pool
    /// </summary>
    public class PersistentTransactionPool : ITransactionPool
    {
        private IPersistentQueue _queue;

        /// <summary>
        /// Initialize a new transaction pool
        //     Throws UnauthorizedAccessException if you do not have read and write permissions.
        //     Throws InvalidOperationException if another instance is attached to the backing store.
        /// </summary>
        public PersistentTransactionPool() => _queue = new PersistentQueue("./transactionpool");

        /// <summary>
        /// Initialize a new transaction pool
        //     Throws UnauthorizedAccessException if you do not have read and write permissions.
        //     Throws InvalidOperationException if another instance is attached to the backing store.
        /// </summary>
        /// <param name="storagePath">Filename / file path to store transaction pool in. Default is a file called "transactionpool"</param>
        /// <param name="maxFilesize">Maximum size in bytes of each file, queue will be rolled into another file once it reaches this</param>
        public PersistentTransactionPool(string storagePath, int maxFilesize) => _queue = new PersistentQueue(storagePath ?? "./transactionpool", maxFilesize);

        /// <summary>
        /// Initialize a new transaction pool
        //     Throws UnauthorizedAccessException if you do not have read and write permissions.
        //     Throws InvalidOperationException if another instance is attached to the backing store.
        /// </summary>
        /// <param name="storagePath">Filename / file path to store transaction pool in. Default is a file called "transactionpool"</param>
        public PersistentTransactionPool(string storagePath) => _queue = new PersistentQueue(storagePath ?? "./transactionpool");

        /// <summary>
        /// Initialize a new transaction pool
        //     Throws UnauthorizedAccessException if you do not have read and write permissions.
        //     Throws InvalidOperationException if another instance is attached to the backing store.
        /// </summary>
        /// <param name="queue">A preconfigured PersistentQueue implementation to use</param>
        public PersistentTransactionPool(IPersistentQueue queue) => _queue = queue;

        /// <summary>
        /// Queue an audit entry to be processed into the blockchain
        /// </summary>
        /// <param name="auditEntry"></param>
        public void AddAuditEntry(IAuditEntry auditEntry)
        {
            using (var session = _queue.OpenSession())
            {
                session.Enqueue(ObjectSerializationExtension.SerializeToByteArray(auditEntry));
                session.Flush();
            }
        }

        /// <summary>
        /// Get the next audit entry from the queue
        /// </summary>
        /// <returns>The next audit entry to </returns>
        public IAuditEntry GetAuditEntry()
        {
            using (var session = _queue.OpenSession())
            {
                var data = session.Dequeue();
                if (data != null)
                {
                    session.Flush();
                    return ObjectSerializationExtension.Deserialize<AuditEntry>(data);
                }
                return null;
            }
        }

        /// <summary>
        /// Close this queue connection. Does not destroy flushed data.
        /// </summary>
        public void Dispose()
        {
            var local = Interlocked.Exchange(ref _queue, null);
            if (local == null) return;
            local.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose of the queue connection on destruction.
        /// This is a safety valve. You should ensure you dispose
        /// of connections properly.
        /// </summary>
        ~PersistentTransactionPool()
        {
            if (_queue == null) return;
            Dispose();
        }
    }
}
