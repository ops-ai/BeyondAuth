using Divergic.Logging.Xunit;
using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Contrib.RavenDB.Stores;
using IdentityServer4.Contrib.RavenDB.Tests.Common;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IdentityServer4.Contrib.RavenDB.Tests
{
    [Collection("IdentityServer4 Tests")]
    public class RavenDBAuthorizationCodeStoreTests : RavenIdentityServerTestBase
    {
        protected readonly ILoggerFactory _loggerFactory;
        private readonly IAuthorizationCodeStore _authorizationCodeStore;
        private readonly IDocumentStore _documentStore;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;
        private string database = Guid.NewGuid().ToString();

        public RavenDBAuthorizationCodeStoreTests(ITestOutputHelper output)
        {
            _loggerFactory = LogFactory.Create(output);
            _documentStore = GetDocumentStore(null);
            _documentStore.EnsureDatabaseExists(database);

            _identityStoreOptions = Microsoft.Extensions.Options.Options.Create(new IdentityStoreOptions { DatabaseName = database });

            _authorizationCodeStore = new RavenDBAuthorizationCodeStore(_loggerFactory.CreateLogger<RavenDBAuthorizationCodeStore>(), _documentStore, _identityStoreOptions);
        }

        [Fact(DisplayName = "GetAuthorizationCodeAsync should return code")]
        public async Task GetAuthorizationCodeAsync()
        {
            using (var session = _documentStore.OpenSession(database))
            {
                session.Store(new AuthorizationCode { ClientId = "1", CreationTime = DateTime.UtcNow }, $"AuthorizationCodes/123");
                session.Store(new AuthorizationCode { ClientId = "3", CreationTime = DateTime.UtcNow }, $"AuthorizationCodes/456");
                session.SaveChanges();
            }

            var code = await _authorizationCodeStore.GetAuthorizationCodeAsync("123");
            Assert.NotNull(code);
            Assert.Equal("1", code.ClientId);
        }

        [Fact(DisplayName = "GetAuthorizationCodeAsync should return null when code doesn't exist")]
        public async Task GetAuthorizationCodeAsyncNoCode()
        {
            using (var session = _documentStore.OpenSession(database))
            {
                session.Store(new AuthorizationCode { ClientId = "1", CreationTime = DateTime.UtcNow }, $"AuthorizationCodes/123");
                session.Store(new AuthorizationCode { ClientId = "3", CreationTime = DateTime.UtcNow }, $"AuthorizationCodes/456");
                session.SaveChanges();
            }

            Assert.Null(await _authorizationCodeStore.GetAuthorizationCodeAsync("124"));
        }

        [Fact(DisplayName = "RemoveAuthorizationCodeAsync should return remove code")]
        public async Task RemoveAuthorizationCodeAsync()
        {
            using (var session = _documentStore.OpenSession(database))
            {
                session.Store(new AuthorizationCode { ClientId = "1", CreationTime = DateTime.UtcNow }, $"AuthorizationCodes/123");
                session.Store(new AuthorizationCode { ClientId = "3", CreationTime = DateTime.UtcNow }, $"AuthorizationCodes/456");
                session.SaveChanges();
            }

            Assert.NotNull(await _authorizationCodeStore.GetAuthorizationCodeAsync("123"));

            await _authorizationCodeStore.RemoveAuthorizationCodeAsync("123");

            Assert.Null(await _authorizationCodeStore.GetAuthorizationCodeAsync("123"));
        }

        [Fact(DisplayName = "StoreAuthorizationCodeAsync should return save consent")]
        public async Task StoreAuthorizationCodeAsync()
        {
            using (var session = _documentStore.OpenSession(database))
            {
                session.Store(new AuthorizationCode { ClientId = "1", CreationTime = DateTime.UtcNow }, $"AuthorizationCodes/123");
                session.Store(new AuthorizationCode { ClientId = "3", CreationTime = DateTime.UtcNow }, $"AuthorizationCodes/456");
                session.SaveChanges();
            }

            var code = await _authorizationCodeStore.StoreAuthorizationCodeAsync(new AuthorizationCode { ClientId = "3", CreationTime = DateTime.UtcNow });

            Assert.NotNull(await _authorizationCodeStore.GetAuthorizationCodeAsync(code));
        }

        [Fact(DisplayName = "Parameter validation should trigger argument exceptions")]
        public async Task Validation()
        {
            Assert.Throws<ArgumentException>(() => new RavenDBAuthorizationCodeStore(null, _documentStore, _identityStoreOptions));
            Assert.Throws<ArgumentException>(() => new RavenDBAuthorizationCodeStore(_loggerFactory.CreateLogger<RavenDBAuthorizationCodeStore>(), null, _identityStoreOptions));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _authorizationCodeStore.GetAuthorizationCodeAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _authorizationCodeStore.RemoveAuthorizationCodeAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _authorizationCodeStore.StoreAuthorizationCodeAsync(null));
        }
    }
}
