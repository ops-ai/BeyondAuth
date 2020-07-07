using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
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
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public RavenDBPersistedGrantStore(IPersistentGrantSerializer serializer, ILogger<RavenDBPersistedGrantStore> logger, IDocumentStore store, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _serializer = serializer;
            _logger = logger ?? throw new ArgumentException("loggerFactory is required", nameof(logger));
            _store = store ?? throw new ArgumentException("store is required", nameof(store));
            _identityStoreOptions = identityStoreOptions;
        }

        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
        {
            if (filter == null)
                throw new ArgumentException("filter is required", nameof(filter));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                _logger.LogDebug($"Getting persisted grants by filter");
                IRavenQueryable<PersistedGrant> query = session.Query<PersistedGrant>();

                if (!string.IsNullOrEmpty(filter.SubjectId))
                    query = query.Where(t => t.SubjectId.Equals(filter.SubjectId));
                if (!string.IsNullOrEmpty(filter.ClientId))
                    query = query.Where(t => t.ClientId.Equals(filter.ClientId));
                if (!string.IsNullOrEmpty(filter.SessionId))
                    query = query.Where(t => t.SessionId.Equals(filter.SessionId));
                if (!string.IsNullOrEmpty(filter.Type))
                    query = query.Where(t => t.Type.Equals(filter.Type));

                return await query.ToListAsync().ConfigureAwait(false);
            }
        }

        public async Task<PersistedGrant> GetAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key is required", nameof(key));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                _logger.LogDebug($"Loading persisted grant {key}");
                return await session.LoadAsync<PersistedGrant>($"PersistedGrants/{key}").ConfigureAwait(false);
            }
        }

        public async Task RemoveAllAsync(PersistedGrantFilter filter)
        {
            if (filter == null)
                throw new ArgumentException("filter is required", nameof(filter));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                _logger.LogDebug($"Getting persisted grants by filter");
                IRavenQueryable<PersistedGrant> query = session.Query<PersistedGrant>();

                if (!string.IsNullOrEmpty(filter.SubjectId))
                    query = query.Where(t => t.SubjectId.Equals(filter.SubjectId));
                if (!string.IsNullOrEmpty(filter.ClientId))
                    query = query.Where(t => t.ClientId.Equals(filter.ClientId));
                if (!string.IsNullOrEmpty(filter.SessionId))
                    query = query.Where(t => t.SessionId.Equals(filter.SessionId));
                if (!string.IsNullOrEmpty(filter.Type))
                    query = query.Where(t => t.Type.Equals(filter.Type));

                var grants = await query.ToListAsync().ConfigureAwait(false);

                grants.ForEach(session.Delete);
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key is required", nameof(key));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
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

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
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
