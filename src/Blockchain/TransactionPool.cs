using System.Collections.Generic;

namespace Blockchain
{
    /// <summary>
    /// Audit entry transaction pool placeholder - to be replaced by distributed queue
    /// </summary>
    public class TransactionPool : ITransactionPool
    {
        private readonly Queue<IAuditEntry> _queue;

        public TransactionPool() => _queue = new Queue<IAuditEntry>();

        public void AddAuditEntry(IAuditEntry auditEntry) => _queue.Enqueue(auditEntry);

        public IAuditEntry GetAuditEntry() => _queue.Dequeue();
    }
}
