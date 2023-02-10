using System.Security.Cryptography.X509Certificates;

namespace AuthorizationServer
{
    public class AuthorizationServerSettings
    {
        public string TenantId { get; set; }

        public string ServerKey { get; set; }

        public X509Certificate2 Certificate { get; set; }

        public string? BaseUrl { get; set; }
    }
}
