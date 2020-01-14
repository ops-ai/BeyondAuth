using System;

namespace Blockchain
{
    /// <summary>
    /// Audit entry transaction pool
    /// </summary>
    public interface ITransactionPool : IDisposable
    {
        void AddAuditEntry(IAuditEntry auditEntry);

        IAuditEntry GetAuditEntry();
    }
}
