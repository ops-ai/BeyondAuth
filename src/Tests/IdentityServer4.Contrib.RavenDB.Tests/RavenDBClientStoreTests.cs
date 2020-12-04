using Divergic.Logging.Xunit;
using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Contrib.RavenDB.Stores;
using IdentityServer4.Contrib.RavenDB.Tests.Common;
using IdentityServer4.Models;
using IdentityServer4.Stores;
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
    public class RavenDBClientStoreTests : RavenIdentityServerTestBase
    {
        protected readonly ILoggerFactory _loggerFactory;
        private readonly IClientStore _clientStore;
        private readonly IDocumentStore _documentStore;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;
        private string database = Guid.NewGuid().ToString();

        public RavenDBClientStoreTests(ITestOutputHelper output)
        {
            _loggerFactory = LogFactory.Create(output);
            _documentStore = GetDocumentStore();
            _documentStore.EnsureDatabaseExists(database);
            _identityStoreOptions = Microsoft.Extensions.Options.Options.Create(new IdentityStoreOptions { DatabaseName = database });

            _clientStore = new RavenDBClientStore(_loggerFactory.CreateLogger<RavenDBClientStore>(), _documentStore, _identityStoreOptions);
        }

        [Fact(DisplayName = "FindClientByIdAsync should return client")]
        public async Task RavenDBClientStoreShouldReturnClient()
        {
            using (var session = _documentStore.OpenSession(database))
            {
                session.Store(new Client { ClientId = "1" }, "Clients/1");
                session.Store(new Client { ClientId = "3" }, "Clients/3");
                session.SaveChanges();
            }

            Assert.NotNull(await _clientStore.FindClientByIdAsync("1"));
        }

        [Fact(DisplayName = "IdentitStore options should be optional and use default database if null")]
        public async Task IdentityStoreOptionsOptional()
        {
            using (var session = _documentStore.OpenSession())
            {
                session.Store(new Client { ClientId = "1" }, "Clients/1");
                session.Store(new Client { ClientId = "3" }, "Clients/3");
                session.SaveChanges();
            }

            var clientStore = new RavenDBClientStore(_loggerFactory.CreateLogger<RavenDBClientStore>(), _documentStore, null);
            Assert.NotNull(await clientStore.FindClientByIdAsync("1"));
        }

        [Fact(DisplayName = "FindClientByIdAsync should return null when client doesn't exist")]
        public async Task RavenDBClientStoreNoClient()
        {
            using (var session = _documentStore.OpenSession(database))
            {
                session.Store(new Client { ClientId = "1" }, "Clients/1");
                session.Store(new Client { ClientId = "3" }, "Clients/3");
                session.SaveChanges();
            }

            Assert.Null(await _clientStore.FindClientByIdAsync("2"));
        }

        [Fact(DisplayName = "Parameter validation should trigger argument exceptions")]
        public async Task Validation()
        {
            Assert.Throws<ArgumentException>(() => new RavenDBClientStore(null, _documentStore, _identityStoreOptions));
            Assert.Throws<ArgumentException>(() => new RavenDBClientStore(_loggerFactory.CreateLogger<RavenDBClientStore>(), null, _identityStoreOptions));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _clientStore.FindClientByIdAsync(null));
        }
    }
}
