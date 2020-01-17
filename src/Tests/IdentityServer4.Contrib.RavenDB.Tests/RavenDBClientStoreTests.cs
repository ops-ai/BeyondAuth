using Divergic.Logging.Xunit;
using IdentityServer4.Contrib.RavenDB.Stores;
using IdentityServer4.Models;
using Microsoft.Extensions.Logging;
using Raven.TestDriver;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IdentityServer4.Contrib.RavenDB.Tests
{
    public class RavenDBClientStoreTests : RavenTestDriver
    {
        protected readonly ILoggerFactory _loggerFactory;

        public RavenDBClientStoreTests(ITestOutputHelper output)
        {
            _loggerFactory = LogFactory.Create(output);
        }

        [Fact(DisplayName = "FindClientByIdAsync should return client")]
        public async Task RavenDBClientStoreShouldReturnClient()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new Client { ClientId = "1" }, "Clients/1");
                    session.Store(new Client { ClientId = "3" }, "Clients/3");
                    session.SaveChanges();
                }

                var clientStore = new RavenDBClientStore(_loggerFactory.CreateLogger<RavenDBClientStore>(), store);
                Assert.NotNull(await clientStore.FindClientByIdAsync("1"));
            }
        }

        [Fact(DisplayName = "FindClientByIdAsync should return null when client doesn't exist")]
        public async Task RavenDBClientStoreNoClient()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new Client { ClientId = "1" }, "Clients/1");
                    session.Store(new Client { ClientId = "3" }, "Clients/3");
                    session.SaveChanges();
                }

                var clientStore = new RavenDBClientStore(_loggerFactory.CreateLogger<RavenDBClientStore>(), store);
                Assert.Null(await clientStore.FindClientByIdAsync("2"));
            }
        }

        [Fact(DisplayName = "Parameter validation should trigger argument exceptions")]
        public async Task Validation()
        {
            using (var store = GetDocumentStore())
            {
                var clientStore = new RavenDBClientStore(_loggerFactory.CreateLogger<RavenDBClientStore>(), store);

                Assert.Throws<ArgumentException>(() => new RavenDBClientStore(null, store));
                Assert.Throws<ArgumentException>(() => new RavenDBClientStore(_loggerFactory.CreateLogger<RavenDBClientStore>(), null));
                await Assert.ThrowsAsync<ArgumentException>(async () => await clientStore.FindClientByIdAsync(null));
            }
        }
    }
}
