using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using System;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RavenDB.Stores
{
    public class ClientStore : IClientStore
    {
        private readonly ILogger _logger;
        private readonly IDocumentStore _store;

        public ClientStore(ILoggerFactory loggerFactory, IDocumentStore store)
        {
            _logger = loggerFactory.CreateLogger<ClientStore>();
            _store = store;
        }

        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("clientId is required", nameof(clientId));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Loading client {clientId}");
                return await session.LoadAsync<Client>(clientId);
            }
        }
    }
}
