using Authentication.Models.Account;
using Finbuckle.MultiTenant;

namespace Authentication.Options
{
    public class TenantSetting : ITenantInfo
    {
        /// <summary>
        /// A unique id for a tenant in the app and should never change
        /// TenantSetting/{host url}
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Account behavior
        /// </summary>
        public AccountOptions AccountOptions { get; set; }

        /// <summary>
        /// Consent behavior
        /// </summary>
        public ConsentOptions ConsentOptions { get; set; }

        /// <summary>
        /// The value used to actually resolve a tenant and should have a syntax compatible for the app (i.e. no crazy symbols in a web app where the identifier will be part of the URL). 
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// Display name for the tenant
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Connection string that should be used for database operations for this tenant. It might connect to a shared database or a dedicated database for the single tenant. 
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Branding options
        /// </summary>
        public BrandingOptions BrandingOptions { get; set; }
    }
}