using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RavenDB.Stores
{
    public class RavenDBResourceStore : IResourceStore
    {
        private readonly ILogger _logger;
        private readonly IDocumentStore _store;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public RavenDBResourceStore(ILogger<RavenDBResourceStore> logger, IDocumentStore store, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _logger = logger ?? throw new ArgumentException("loggerFactory is required", nameof(logger));
            _store = store ?? throw new ArgumentException("store is required", nameof(store));
            _identityStoreOptions = identityStoreOptions;
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
        {
            if (apiResourceNames == null || !apiResourceNames.Any())
                throw new ArgumentException("apiResourceName is required", nameof(apiResourceNames));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                _logger.LogDebug($"Loading api resource {string.Join(", ", apiResourceNames)}");
                var apiResources = await session.LoadAsync<ApiResource>(apiResourceNames.Select(t => $"ApiResources/{t}").ToList()).ConfigureAwait(false);
                return apiResources.Where(t => t.Value != null).Select(t => t.Value);
            }
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            if (scopeNames == null)
                throw new ArgumentException("scopeNames is required", nameof(scopeNames));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                _logger.LogDebug($"Loading api resources with scopes {string.Join(",", scopeNames)}");
                return await session.Query<ApiResource>().Where(t => t.Scopes.Any(s => s.In(scopeNames))).Take(1024).ToListAsync().ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
        {
            if (scopeNames == null)
                throw new ArgumentException("scopeNames is required", nameof(scopeNames));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                _logger.LogDebug($"Loading api resource {string.Join(", ", scopeNames)}");
                var apiResources = await session.LoadAsync<ApiScope>(scopeNames.Select(t => $"Scopes/{t}").ToList()).ConfigureAwait(false);
                return apiResources.Where(t => t.Value != null).Select(t => t.Value);
            }
        }

        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            if (scopeNames == null)
                throw new ArgumentException("scopeNames is required", nameof(scopeNames));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                _logger.LogDebug($"Loading identity resources with scopes {string.Join(",", scopeNames)}");
                return await session.Query<IdentityResource>().Where(t => t.Name.In(scopeNames)).Take(1024).ToListAsync().ConfigureAwait(false);
            }
        }

        public async Task<Resources> GetAllResourcesAsync()
        {
            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                _logger.LogDebug($"Loading all resources");
                var apiResources = await session.Query<ApiResource>().Take(1024).ToListAsync().ConfigureAwait(false);
                var identityResources = await session.Query<IdentityResource>().Take(1024).ToListAsync().ConfigureAwait(false);
                var apiScopes = await session.Query<ApiScope>().Take(1024).ToListAsync().ConfigureAwait(false);

                return new Resources(identityResources, apiResources, apiScopes);
            }
        }
    }
}
