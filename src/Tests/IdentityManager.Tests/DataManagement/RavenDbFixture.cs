using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IdentityManager.Tests.DataManagement
{
    public class RavenDbFixture : IDisposable
    {
        public IDocumentStore Store { get; }

        public RavenDbFixture()
        {
            var variables = new[]
               {
                new { Name = "VaultUri", Value = "" }
            };
            foreach (var variable in variables)
            {
                Environment.SetEnvironmentVariable(variable.Name, variable.Value.ToString());
            }

            var client = new CertificateClient(vaultUri: new Uri(Environment.GetEnvironmentVariable("VaultUri")), credential: new DefaultAzureCredential());
            var secretClient = new SecretClient(new Uri(Environment.GetEnvironmentVariable("VaultUri")), new DefaultAzureCredential());

            var certResponse = client.GetCertificateAsync("RavenDB").ConfigureAwait(false).GetAwaiter().GetResult();
            var secretId = certResponse.Value.SecretId;
            var segments = secretId.Segments;
            var secretName = segments[2].Trim('/');
            var version = segments[3].TrimEnd('/');

            var secretResponse = secretClient.GetSecretAsync(secretName, version).ConfigureAwait(false).GetAwaiter().GetResult();

            var secret = secretResponse.Value;
            var privateKeyBytes = Convert.FromBase64String(secret.Value);
            var cert = new X509Certificate2(privateKeyBytes);

            Store = new DocumentStore
            {
                Database = "",
                Urls = new[] { "" },
                Certificate = cert
            };

            var oldResolver = Store.Conventions.FindCollectionName;
            Store.Conventions.FindCollectionName += (type) =>
            {
                if (type.Name.StartsWith("SiteSetting"))
                    return "SiteSettings";

                return oldResolver(type);
            };

            Store = Store.Initialize();
        }

        public void Dispose()
        {
            // ... clean up test data from the database ...
        }
    }
}
