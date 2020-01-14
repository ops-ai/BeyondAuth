using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Blockchain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;

namespace AuditServer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AuditController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly ITransactionPool _transactionPool;
        private readonly ILogger<AuditController> _logger;

        public AuditController(ILoggerFactory factory, IDocumentStore documentStore, ITransactionPool transactionPool)
        {
            _logger = factory.CreateLogger<AuditController>();
            _documentStore = documentStore;
            _transactionPool = transactionPool;
        }

        [HttpPost]
        public async Task<IActionResult> Post(AuditEntry auditEntry)
        {
            try
            {
                //TODO: Add fluent validation

                var clientId = User.FindFirstValue("client_id");
                auditEntry.ClientId = clientId;
                _transactionPool.AddAuditEntry(auditEntry);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue audit entry");
                throw;
            }
        }
    }
}
