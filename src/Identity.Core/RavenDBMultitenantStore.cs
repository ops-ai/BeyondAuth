using Finbuckle.MultiTenant;
using Microsoft.Extensions.Caching.Memory;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Identity.Core
{
    /// <summary>
    /// RavenDB store for multitenant settings
    /// </summary>
    public class RavenDBMultitenantStore : IMultiTenantStore<TenantSetting>
    {
        private IDocumentStore _store;
        private IMemoryCache _cache;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="store"></param>
        /// <param name="memoryCache"></param>
        public RavenDBMultitenantStore(IDocumentStore store, IMemoryCache memoryCache)
        {
            _store = store;
            _cache = memoryCache;

            _store.Changes().ForDocumentsInCollection<TenantSetting>().Subscribe(change =>
            {
                _cache.Remove($"TenantSettingId-{change.Id.Split('/').Last()}");
                _cache.Remove($"TenantSetting-{change.Id.Split('/').Last()}");
            });
        }

        public async Task<IEnumerable<TenantSetting>> GetAllAsync()
        {
            using (var session = _store.OpenAsyncSession())
            {
                var tenants = await session.Query<TenantSetting>().ToListAsync();
                tenants.ForEach(t => t.Id = t.Id.Split('/').Last());
                return tenants;
            }
        }

        /// <summary>
        /// Add a new tenant
        /// </summary>
        /// <param name="tenantInfo"></param>
        /// <returns></returns>
        public async Task<bool> TryAddAsync(TenantSetting tenantInfo)
        {
            using (var session = _store.OpenAsyncSession())
            {
                if (await session.Advanced.ExistsAsync($"TenantSettings/{tenantInfo.Id}"))
                    return false;

                //TODO: unique constraint on identifier, property validation?
                tenantInfo.Id = $"TenantSettings/{tenantInfo.Id}";

                await session.StoreAsync(tenantInfo);
                await session.SaveChangesAsync();

                return true;
            }
        }

        /// <summary>
        /// Get a tenant by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<TenantSetting> TryGetAsync(string id)
        {
            if (!_cache.TryGetValue($"TenantSettingId-{id}", out TenantSetting cachedTenant))
            {
                using (var session = _store.OpenAsyncSession())
                {
                    cachedTenant = await session.LoadAsync<TenantSetting>($"TenantSettings/{id}");
                    cachedTenant.Id = cachedTenant.Id.Split('/').Last();
                }

                _cache.Set($"TenantSettingId-{id}", cachedTenant, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(1)));
            }
            return cachedTenant;
        }

        /// <summary>
        /// Find a tenant by identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public async Task<TenantSetting> TryGetByIdentifierAsync(string identifier)
        {
            if (!_cache.TryGetValue($"TenantSetting-{identifier}", out TenantSetting cachedTenant))
            {
                using (var session = _store.OpenAsyncSession())
                {
                    cachedTenant = await session.Query<TenantSetting>().FirstOrDefaultAsync(t => t.Identifier.Equals(identifier));
                    if (cachedTenant == null)
                        return null;

                    cachedTenant.Id = cachedTenant.Id.Split('/').Last();
                }

                _cache.Set($"TenantSetting-{identifier}", cachedTenant, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(1)));
            }
            return cachedTenant;
        }

        /// <summary>
        /// Remove tenant settings
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> TryRemoveAsync(string id)
        {
            using (var session = _store.OpenAsyncSession())
            {
                var tenant = await session.LoadAsync<TenantSetting>($"TenantSettings/{id}");
                if (tenant != null)
                {
                    session.Delete(tenant);
                    await session.SaveChangesAsync();

                    _cache.Remove($"TenantSetting-{tenant.Identifier}");
                    _cache.Remove($"TenantSettingId-{tenant.Id.Split('/').Last()}");

                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Update tenant
        /// </summary>
        /// <param name="tenantInfo"></param>
        /// <returns></returns>
        public async Task<bool> TryUpdateAsync(TenantSetting tenantInfo)
        {
            using (var session = _store.OpenAsyncSession())
            {
                await session.StoreAsync(tenantInfo);
                await session.SaveChangesAsync();

                _cache.Remove($"TenantSetting-{tenantInfo.Identifier}");
                _cache.Remove($"TenantSettingId-{tenantInfo.Id.Split('/').Last()}");

                return true;
            }
        }
    }
}
