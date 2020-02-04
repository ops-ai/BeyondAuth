using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RavenDB.Services
{
    public class CorsPolicyService : ICorsPolicyService
    {
        private readonly ILogger<CorsPolicyService> _logger;
        private readonly IDocumentStore _store;

        public CorsPolicyService(ILogger<CorsPolicyService> logger, IDocumentStore store)
        {
            _logger = logger;
            _store = store;
        }

        public async Task<bool> IsOriginAllowedAsync(string origin)
        {
            using (var session = _store.OpenAsyncSession())
            {
                var isAllowed = await session.Query<Client>().AnyAsync(t => t.AllowedCorsOrigins.Any(c => c == origin)).ConfigureAwait(false);

                _logger.LogDebug("Origin {origin} is allowed: {originAllowed}", origin, isAllowed);

                return isAllowed;
            }
        }
    }
}
