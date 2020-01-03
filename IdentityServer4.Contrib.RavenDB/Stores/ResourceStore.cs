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
    public class ResourceStore : IResourceStore
    {
        private readonly ILogger _logger;
        private readonly IDocumentStore _store;

        public ResourceStore(ILoggerFactory loggerFactory, IDocumentStore store)
        {
            _logger = loggerFactory.CreateLogger<ResourceStore>();
            _store = store;
        }

        public async Task<ApiResource> FindApiResourceAsync(string name)
        {
            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Loading api resource {name} from document store");
                return await session.LoadAsync<ApiResource>(name);
            }
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Loading api resources with scopes {string.Join(",", scopeNames)} from document store");
                return await session.Query<ApiResource>().Where(t => t.Scopes.Any(s => scopeNames.Contains(s.Name))).Take(1024).ToListAsync();
            }
        }

        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Loading identity resources with scopes {string.Join(",", scopeNames)} from document store");
                return await session.Query<IdentityResource>().Where(t => t.Name.In(scopeNames)).Take(1024).ToListAsync();
            }
        }

        public async Task<Resources> GetAllResourcesAsync()
        {
            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Loading all resources from document store");
                var apiResources = session.Query<ApiResource>().Take(1024).ToListAsync();
                var identityResources = session.Query<IdentityResource>().Take(1024).ToListAsync();

                await Task.WhenAll(apiResources, identityResources);

                return new Resources(identityResources.Result, apiResources.Result);
            }
        }
    }
}
