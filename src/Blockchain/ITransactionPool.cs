namespace Blockchain
{
    /// <summary>
    /// Audit entry transaction pool
    /// </summary>
    public interface ITransactionPool
    {
        void AddAuditEntry(IAuditEntry auditEntry);

        IAuditEntry GetAuditEntry();
    }
}
