using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RavenDB.Stores
{
    public class RavenDBResourceStore : IResourceStore
    {
        private readonly ILogger _logger;
        private readonly IDocumentStore _store;

        public RavenDBResourceStore(ILogger<RavenDBResourceStore> logger, IDocumentStore store)
        {
            _logger = logger;
            _store = store;
        }

        public async Task<ApiResource> FindApiResourceAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name is required", nameof(name));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Loading api resource {name}");
                return await session.LoadAsync<ApiResource>($"ApiResources/{name}");
            }
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            if (scopeNames == null)
                throw new ArgumentException("scopeNames is required", nameof(scopeNames));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Loading api resources with scopes {string.Join(",", scopeNames)}");
                return await session.Query<ApiResource>().Where(t => t.Scopes.Any(s => s.Name.In(scopeNames))).Take(1024).ToListAsync();
            }
        }

        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            if (scopeNames == null)
                throw new ArgumentException("scopeNames is required", nameof(scopeNames));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Loading identity resources with scopes {string.Join(",", scopeNames)}");
                return await session.Query<IdentityResource>().Where(t => t.Name.In(scopeNames)).Take(1024).ToListAsync();
            }
        }

        public async Task<Resources> GetAllResourcesAsync()
        {
            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Loading all resources");
                var apiResources = await session.Query<ApiResource>().Take(1024).ToListAsync();
                var identityResources = await session.Query<IdentityResource>().Take(1024).ToListAsync();

                return new Resources(identityResources, apiResources);
            }
        }
    }
}
