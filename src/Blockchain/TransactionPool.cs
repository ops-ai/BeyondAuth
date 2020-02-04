using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Blockchain
{
    /// <summary>
    /// Audit entry transaction pool placeholder - to be replaced by distributed queue
    /// </summary>
    public class TransactionPool : ITransactionPool
    {
        private ConcurrentQueue<IAuditEntry> _queue;

        public TransactionPool() => _queue = new ConcurrentQueue<IAuditEntry>();

        public void AddAuditEntry(IAuditEntry auditEntry) => _queue.Enqueue(auditEntry);

        public IAuditEntry GetAuditEntry()
        {
            if (_queue.TryDequeue(out var entry))
                return entry;

            return null;
        }

        /// <summary>
        /// Close this queue connection. Does not destroy flushed data.
        /// </summary>
        public void Dispose()
        {
            var local = Interlocked.Exchange(ref _queue, null);
            if (local == null) return;

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose of the queue connection on destruction.
        /// This is a safety valve. You should ensure you dispose
        /// of connections properly.
        /// </summary>
        ~TransactionPool()
        {
            if (_queue == null) return;
            Dispose();
        }
    }
}
