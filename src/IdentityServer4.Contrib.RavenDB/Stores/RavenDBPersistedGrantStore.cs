using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
using Microsoft.Extensions.Logging;
using Raven.Client;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RavenDB.Stores
{
    public class RavenDBPersistedGrantStore : IPersistedGrantStore
    {
        private readonly ILogger _logger;
        private readonly IPersistentGrantSerializer _serializer;
        private readonly IDocumentStore _store;

        public RavenDBPersistedGrantStore(IPersistentGrantSerializer serializer, ILogger<RavenDBPersistedGrantStore> logger, IDocumentStore store)
        {
            _serializer = serializer;
            _logger = logger ?? throw new ArgumentException("loggerFactory is required", nameof(logger));
            _store = store ?? throw new ArgumentException("store is required", nameof(store));
        }

        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            if (string.IsNullOrEmpty(subjectId))
                throw new ArgumentException("subjectId is required", nameof(subjectId));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Getting persisted grants for subjectId {subjectId}");
                return await session.Query<PersistedGrant>().Where(t => t.SubjectId.Equals(subjectId)).ToListAsync().ConfigureAwait(false);
            }
        }

        public async Task<PersistedGrant> GetAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key is required", nameof(key));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Loading persisted grant {key}");
                return await session.LoadAsync<PersistedGrant>($"PersistedGrants/{key}").ConfigureAwait(false);
            }
        }

        public async Task RemoveAllAsync(string subjectId, string clientId)
        {
            if (string.IsNullOrEmpty(subjectId))
                throw new ArgumentException("subjectId is required", nameof(subjectId));

            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("clientId is required", nameof(clientId));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Deleting persisted grants for subjectId {subjectId} and clientId {clientId}");
                var grants = await session.Query<PersistedGrant>().Where(t => t.SubjectId.Equals(subjectId) && t.ClientId.Equals(clientId)).ToListAsync().ConfigureAwait(false);
                grants.ForEach(session.Delete);
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            if (string.IsNullOrEmpty(subjectId))
                throw new ArgumentException("subjectId is required", nameof(subjectId));

            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("clientId is required", nameof(clientId));

            if (string.IsNullOrEmpty(type))
                throw new ArgumentException("type is required", nameof(type));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Deleting persisted grants for subjectId {subjectId}, clientId {clientId} and type {type}");
                var grants = await session.Query<PersistedGrant>().Where(t => t.SubjectId.Equals(subjectId) && t.ClientId.Equals(clientId) && t.Type.Equals(type)).ToListAsync().ConfigureAwait(false);
                grants.ForEach(session.Delete);
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key is required", nameof(key));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Deleting persisted grant {key}");
                session.Delete($"PersistedGrants/{key}");
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task StoreAsync(PersistedGrant grant)
        {
            if (grant == null)
                throw new ArgumentException("grant is required", nameof(grant));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Storing persisted grant {grant.Key}");
                await session.StoreAsync(grant, $"PersistedGrants/{grant.Key}").ConfigureAwait(false);

                if (grant.Expiration.HasValue)
                    session.Advanced.GetMetadataFor(grant)[Constants.Documents.Metadata.Expires] = grant.Expiration;

                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}
