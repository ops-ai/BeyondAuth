using Divergic.Logging.Xunit;
using IdentityServer4.Contrib.RavenDB.Stores;
using IdentityServer4.Models;
using Microsoft.Extensions.Logging;
using Raven.TestDriver;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IdentityServer4.Contrib.RavenDB.Tests
{
    public class RavenDBAuthorizationCodeStoreTests : RavenTestDriver
    {
        protected readonly ILoggerFactory _loggerFactory;

        public RavenDBAuthorizationCodeStoreTests(ITestOutputHelper output)
        {
            _loggerFactory = LogFactory.Create(output);
        }

        [Fact(DisplayName = "GetAuthorizationCodeAsync should return code")]
        public async Task GetAuthorizationCodeAsync()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new AuthorizationCode { ClientId = "1", CreationTime = DateTime.UtcNow }, $"AuthorizationCodes/123");
                    session.Store(new AuthorizationCode { ClientId = "3", CreationTime = DateTime.UtcNow }, $"AuthorizationCodes/456");
                    session.SaveChanges();
                }

                var codeStore = new RavenDBAuthorizationCodeStore(_loggerFactory.CreateLogger<RavenDBAuthorizationCodeStore>(), store);
                var code = await codeStore.GetAuthorizationCodeAsync("123");
                Assert.NotNull(code);
                Assert.Equal("1", code.ClientId);
            }
        }

        [Fact(DisplayName = "GetAuthorizationCodeAsync should return null when code doesn't exist")]
        public async Task GetAuthorizationCodeAsyncNoCode()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new AuthorizationCode { ClientId = "1", CreationTime = DateTime.UtcNow }, $"AuthorizationCodes/123");
                    session.Store(new AuthorizationCode { ClientId = "3", CreationTime = DateTime.UtcNow }, $"AuthorizationCodes/456");
                    session.SaveChanges();
                }

                var codeStore = new RavenDBAuthorizationCodeStore(_loggerFactory.CreateLogger<RavenDBAuthorizationCodeStore>(), store);
                Assert.Null(await codeStore.GetAuthorizationCodeAsync("124"));
            }
        }

        [Fact(DisplayName = "RemoveAuthorizationCodeAsync should return remove code")]
        public async Task RemoveAuthorizationCodeAsync()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new AuthorizationCode { ClientId = "1", CreationTime = DateTime.UtcNow }, $"AuthorizationCodes/123");
                    session.Store(new AuthorizationCode { ClientId = "3", CreationTime = DateTime.UtcNow }, $"AuthorizationCodes/456");
                    session.SaveChanges();
                }

                var codeStore = new RavenDBAuthorizationCodeStore(_loggerFactory.CreateLogger<RavenDBAuthorizationCodeStore>(), store);
                Assert.NotNull(await codeStore.GetAuthorizationCodeAsync("123"));

                await codeStore.RemoveAuthorizationCodeAsync("123");

                Assert.Null(await codeStore.GetAuthorizationCodeAsync("123"));
            }
        }

        [Fact(DisplayName = "StoreAuthorizationCodeAsync should return save consent")]
        public async Task StoreAuthorizationCodeAsync()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new AuthorizationCode { ClientId = "1", CreationTime = DateTime.UtcNow }, $"AuthorizationCodes/123");
                    session.Store(new AuthorizationCode { ClientId = "3", CreationTime = DateTime.UtcNow }, $"AuthorizationCodes/456");
                    session.SaveChanges();
                }

                var codeStore = new RavenDBAuthorizationCodeStore(_loggerFactory.CreateLogger<RavenDBAuthorizationCodeStore>(), store);

                var code = await codeStore.StoreAuthorizationCodeAsync(new AuthorizationCode { ClientId = "3", CreationTime = DateTime.UtcNow });

                Assert.NotNull(await codeStore.GetAuthorizationCodeAsync(code));
            }
        }
    }
}
