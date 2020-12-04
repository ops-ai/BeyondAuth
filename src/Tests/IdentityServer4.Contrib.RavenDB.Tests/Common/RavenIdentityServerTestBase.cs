using IdentityServer4.Stores.Serialization;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.Json.Serialization.NewtonsoftJson;
using Raven.TestDriver;
using System.Threading;

namespace IdentityServer4.Contrib.RavenDB.Tests.Common
{
    public class RavenIdentityServerTestBase : RavenTestDriver
    {
        protected override void PreInitialize(IDocumentStore documentStore)
        {
            var serializerConventions = new NewtonsoftJsonSerializationConventions();
            serializerConventions.CustomizeJsonSerializer += (JsonSerializer serializer) =>
            {
                serializer.Converters.Add(new ClaimConverter());
                serializer.Converters.Add(new ClaimsPrincipalConverter());
            };

            documentStore.Conventions.Serialization = serializerConventions;
        }
    }
}
