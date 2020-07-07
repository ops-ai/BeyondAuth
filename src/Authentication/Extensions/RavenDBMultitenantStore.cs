using Authentication.Options;
using Finbuckle.MultiTenant;
using Raven.Client.Documents;
using System.Threading.Tasks;

namespace Authentication.Extensions
{
    /// <summary>
    /// RavenDB store for multitenant settings
    /// </summary>
    public class RavenDBMultitenantStore : IMultiTenantStore<TenantSetting>
    {
        private IDocumentStore _store;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="store"></param>
        public RavenDBMultitenantStore(IDocumentStore store)
        {
            _store = store;
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
            using (var session = _store.OpenAsyncSession())
            {
                return await session.LoadAsync<TenantSetting>(id);
            }
        }

        /// <summary>
        /// Find a tenant by identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public async Task<TenantSetting> TryGetByIdentifierAsync(string identifier)
        {
            using (var session = _store.OpenAsyncSession())
            {
                var tenant = await session.Query<TenantSetting>().FirstOrDefaultAsync(t => t.Identifier.Equals(identifier));
                if (tenant == null)
                    await TryAddAsync(new TenantSetting { Id = $"TenantSettings/{identifier}", Identifier = identifier, Name = identifier });
                return tenant;
            }
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

                return true;
            }
        }
    }
}
