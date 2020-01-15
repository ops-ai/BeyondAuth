using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using shortid;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RavenDB.Stores
{
    public class RavenDBReferenceTokenStore : IReferenceTokenStore
    {
        private readonly ILogger _logger;
        private readonly IDocumentStore _store;

        public RavenDBReferenceTokenStore(ILoggerFactory loggerFactory, IDocumentStore store)
        {
            _logger = loggerFactory.CreateLogger<RavenDBReferenceTokenStore>();
            _store = store;
        }

        public async Task<Token> GetReferenceTokenAsync(string handle)
        {
            if (string.IsNullOrEmpty(handle))
                throw new ArgumentException("handle is required", nameof(handle));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Loading reference token {handle}");
                return await session.LoadAsync<Token>($"ReferenceTokens/{handle}");
            }
        }

        public async Task RemoveReferenceTokenAsync(string handle)
        {
            if (string.IsNullOrEmpty(handle))
                throw new ArgumentException("handle is required", nameof(handle));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Deleting reference token {handle}");
                session.Delete($"ReferenceTokens/{handle}");
                await session.SaveChangesAsync();
            }
        }

        public async Task RemoveReferenceTokensAsync(string subjectId, string clientId)
        {
            if (string.IsNullOrEmpty(subjectId))
                throw new ArgumentException("subjectId is required", nameof(subjectId));

            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("clientId is required", nameof(clientId));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Deleting reference tokens for subjectId {subjectId} and clientId {clientId}");
                var grants = await session.Query<Token>().Where(t => t.SubjectId.Equals(subjectId) && t.ClientId.Equals(clientId)).ToListAsync();
                grants.ForEach(session.Delete);
                await session.SaveChangesAsync();
            }
        }

        public async Task<string> StoreReferenceTokenAsync(Token token)
        {
            if (token != null)
                throw new ArgumentException("token is required", nameof(token));

            using (var session = _store.OpenAsyncSession())
            {
                var newCode = ShortId.Generate(true, false, 14);
                _logger.LogDebug($"Storing reference token {newCode}");
                await session.StoreAsync(token, $"ReferenceTokens/{newCode}");
                await session.SaveChangesAsync();

                return newCode;
            }
        }
    }
}
