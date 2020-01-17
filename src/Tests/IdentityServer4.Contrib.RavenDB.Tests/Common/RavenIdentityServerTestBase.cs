using IdentityServer4.Stores.Serialization;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.TestDriver;

namespace IdentityServer4.Contrib.RavenDB.Tests.Common
{
    public class RavenIdentityServerTestBase : RavenTestDriver
    {
        protected override void PreInitialize(IDocumentStore documentStore)
        {
            documentStore.Conventions.CustomizeJsonSerializer += (JsonSerializer serializer) =>
            {
                serializer.Converters.Add(new ClaimConverter());
                serializer.Converters.Add(new ClaimsPrincipalConverter());
            };
        }
    }
}
