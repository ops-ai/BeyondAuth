using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using shortid;
using shortid.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RavenDB.Stores
{
    public class RavenDBReferenceTokenStore : IReferenceTokenStore
    {
        private readonly ILogger _logger;
        private readonly IDocumentStore _store;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public RavenDBReferenceTokenStore(ILogger<RavenDBReferenceTokenStore> logger, IDocumentStore store, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _logger = logger ?? throw new ArgumentException("loggerFactory is required", nameof(logger));
            _store = store ?? throw new ArgumentException("store is required", nameof(store));
            _identityStoreOptions = identityStoreOptions;
        }

        public async Task<Token> GetReferenceTokenAsync(string handle)
        {
            if (string.IsNullOrEmpty(handle))
                throw new ArgumentException("handle is required", nameof(handle));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                _logger.LogDebug($"Loading reference token {handle}");
                return await session.LoadAsync<Token>($"ReferenceTokens/{handle}").ConfigureAwait(false);
            }
        }

        public async Task RemoveReferenceTokenAsync(string handle)
        {
            if (string.IsNullOrEmpty(handle))
                throw new ArgumentException("handle is required", nameof(handle));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                _logger.LogDebug($"Deleting reference token {handle}");
                session.Delete($"ReferenceTokens/{handle}");
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task RemoveReferenceTokensAsync(string subjectId, string clientId)
        {
            if (string.IsNullOrEmpty(subjectId))
                throw new ArgumentException("subjectId is required", nameof(subjectId));

            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("clientId is required", nameof(clientId));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                _logger.LogDebug($"Deleting reference tokens for subjectId {subjectId} and clientId {clientId}");
                var grants = await session.Query<Token>().Where(t => t.SubjectId.Equals(subjectId) && t.ClientId.Equals(clientId)).ToListAsync().ConfigureAwait(false);
                grants.ForEach(session.Delete);
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task<string> StoreReferenceTokenAsync(Token token)
        {
            if (token == null)
                throw new ArgumentException("token is required", nameof(token));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                var newCode = ShortId.Generate(new GenerationOptions { Length = 14, UseNumbers = true, UseSpecialCharacters = false });
                _logger.LogDebug($"Storing reference token {newCode}");
                await session.StoreAsync(token, $"ReferenceTokens/{newCode}").ConfigureAwait(false);
                await session.SaveChangesAsync().ConfigureAwait(false);

                return newCode;
            }
        }
    }
}
