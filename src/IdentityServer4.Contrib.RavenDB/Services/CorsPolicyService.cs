using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RavenDB.Services
{
    public class CorsPolicyService : ICorsPolicyService
    {
        private readonly ILogger<CorsPolicyService> _logger;
        private readonly IDocumentStore _store;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public CorsPolicyService(ILogger<CorsPolicyService> logger, IDocumentStore store, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _logger = logger;
            _store = store;
            _identityStoreOptions = identityStoreOptions;
        }

        public async Task<bool> IsOriginAllowedAsync(string origin)
        {
            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                var isAllowed = await session.Query<Client>().AnyAsync(t => t.AllowedCorsOrigins.Any(c => c == origin)).ConfigureAwait(false);

                _logger.LogDebug("Origin {origin} is allowed: {originAllowed}", origin, isAllowed);

                return isAllowed;
            }
        }
    }
}
