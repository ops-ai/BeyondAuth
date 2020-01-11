using IdentityServer4.Models;

namespace IdentityServer4.Contrib.RavenDB.Entities
{
    public class DeviceCodeEntity : DeviceCode
    {
        /// <summary>
        /// User code
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Device code
        /// </summary>
        public string DeviceCode { get; set; }
    }
}
