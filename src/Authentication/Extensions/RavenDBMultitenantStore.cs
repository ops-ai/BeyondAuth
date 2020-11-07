using Authentication.Options;
using Finbuckle.MultiTenant;
using Microsoft.Extensions.Caching.Memory;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Authentication.Extensions
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
        }

        public async Task<IEnumerable<TenantSetting>> GetAllAsync()
        {
            using (var session = _store.OpenAsyncSession())
                return await session.Query<TenantSetting>().ToListAsync();
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
                if (await session.Advanced.ExistsAsync(tenantInfo.Id))
                    return false;

                //TODO: unique constraint on identifier, property validation?

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
                    cachedTenant = await session.LoadAsync<TenantSetting>(id);

                _cache.Set($"TenantSettingId-{id}", cachedTenant, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(30)));
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
                    cachedTenant = await session.Query<TenantSetting>().FirstOrDefaultAsync(t => t.Identifier.Equals(identifier));

                _cache.Set($"TenantSettingId-{identifier}", cachedTenant, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(30)));
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
                var tenant = await session.LoadAsync<TenantSetting>(id);
                if (tenant != null)
                {
                    session.Delete(tenant);
                    await session.SaveChangesAsync();

                    _cache.Remove($"TenantSetting-{tenant.Identifier}");
                    _cache.Remove($"TenantSetting-{tenant.Id}");
                    
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
                _cache.Remove($"TenantSetting-{tenantInfo.Id}");

                return true;
            }
        }
    }
}
