using Divergic.Logging.Xunit;
using FluentAssertions;
using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Contrib.RavenDB.Services;
using IdentityServer4.Contrib.RavenDB.Tests.Common;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IdentityServer4.Contrib.RavenDB.Tests
{
    [Collection("IdentityServer4 Tests")]
    public class RavenDBCorsServiceTests : RavenIdentityServerTestBase
    {
        protected readonly ILoggerFactory _loggerFactory;
        private readonly ICorsPolicyService _corsService;
        private readonly IDocumentStore _documentStore;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;
        private string database = Guid.NewGuid().ToString();

        public RavenDBCorsServiceTests(ITestOutputHelper output)
        {
            _loggerFactory = LogFactory.Create(output);
            _documentStore = GetDocumentStore();
            _documentStore.EnsureDatabaseExists(database);
            _identityStoreOptions = Microsoft.Extensions.Options.Options.Create(new IdentityStoreOptions { DatabaseName = database });

            _corsService = new CorsPolicyService(_loggerFactory.CreateLogger<CorsPolicyService>(), _documentStore, _identityStoreOptions);
        }

        [Fact(DisplayName = "IsOriginAllowedAsync should return determine allowed cors")]
        public async Task CorsShouldResolveCorrectly()
        {
            using (var session = _documentStore.OpenSession(database))
            {
                var client = new Client { ClientId = "1" };
                client.AllowedCorsOrigins.Add("http://example.com");
                session.Store(client, "Clients/1");
                session.Store(new Client { ClientId = "3" }, "Clients/3");
                session.SaveChanges();
            }

            var result = await _corsService.IsOriginAllowedAsync("http://example.com");
            result.Should().BeTrue();

            result = await _corsService.IsOriginAllowedAsync("http://example2.com");
            result.Should().BeFalse();
        }
    }
}
