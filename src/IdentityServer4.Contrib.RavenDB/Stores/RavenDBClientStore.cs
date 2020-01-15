using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using System;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RavenDB.Stores
{
    public class RavenDBClientStore : IClientStore
    {
        private readonly ILogger _logger;
        private readonly IDocumentStore _store;

        public RavenDBClientStore(ILogger<RavenDBClientStore> logger, IDocumentStore store)
        {
            _logger = logger ?? throw new ArgumentException("loggerFactory is required", nameof(logger));
            _store = store ?? throw new ArgumentException("store is required", nameof(store));
        }

        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("clientId is required", nameof(clientId));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Loading client {clientId}");
                return await session.LoadAsync<Client>($"Clients/{clientId}");
            }
        }
    }
}
