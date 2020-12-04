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
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IdentityServer4.Contrib.RavenDB.Tests
{
    [Collection("IdentityServer4 Tests")]
    public class RavenDBUserConsentStoreTests : RavenIdentityServerTestBase
    {
        protected readonly ILoggerFactory _loggerFactory;
        private readonly IUserConsentStore _userConsentStore;
        private readonly IDocumentStore _documentStore;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;
        private string database = Guid.NewGuid().ToString();

        public RavenDBUserConsentStoreTests(ITestOutputHelper output)
        {
            _loggerFactory = LogFactory.Create(output);
            _documentStore = GetDocumentStore();
            _documentStore.EnsureDatabaseExists(database);
            _identityStoreOptions = Microsoft.Extensions.Options.Options.Create(new IdentityStoreOptions { DatabaseName = database });

            _userConsentStore = new RavenDBUserConsentStore(_loggerFactory.CreateLogger<RavenDBUserConsentStore>(), _documentStore, _identityStoreOptions);
        }

        [Fact(DisplayName = "GetUserConsentAsync should return consent")]
        public async Task GetUserConsentAsync()
        {
            using (var session = _documentStore.OpenSession(database))
            {
                session.Store(new Consent { ClientId = "1", CreationTime = DateTime.UtcNow, SubjectId = "test@test.com" }, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes("1"))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes("test@test.com"))}");
                session.Store(new Consent { ClientId = "3", CreationTime = DateTime.UtcNow, SubjectId = "test@test.com" }, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes("3"))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes("test@test.com"))}");
                session.SaveChanges();
            }

            Assert.NotNull(await _userConsentStore.GetUserConsentAsync("test@test.com", "1"));
        }

        [Fact(DisplayName = "GetUserConsentAsync should return null when consent doesn't exist")]
        public async Task RavenDBconsentStoreNoClient()
        {
            using (var session = _documentStore.OpenSession(database))
            {
                session.Store(new Consent { ClientId = "1", CreationTime = DateTime.UtcNow, SubjectId = "test@test.com" }, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes("1"))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes("test@test.com"))}");
                session.Store(new Consent { ClientId = "3", CreationTime = DateTime.UtcNow, SubjectId = "test@test.com" }, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes("3"))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes("test@test.com"))}");
                session.SaveChanges();
            }

            Assert.Null(await _userConsentStore.GetUserConsentAsync("nothing@test.com", "1"));
            Assert.Null(await _userConsentStore.GetUserConsentAsync("test@test.com", "2"));
        }

        [Fact(DisplayName = "RemoveUserConsentAsync should return remove consent")]
        public async Task RemoveUserConsentAsync()
        {
            using (var session = _documentStore.OpenSession(database))
            {
                session.Store(new Consent { ClientId = "1", CreationTime = DateTime.UtcNow, SubjectId = "test@test.com" }, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes("1"))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes("test@test.com"))}");
                session.Store(new Consent { ClientId = "3", CreationTime = DateTime.UtcNow, SubjectId = "test@test.com" }, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes("3"))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes("test@test.com"))}");
                session.SaveChanges();
            }

            Assert.NotNull(await _userConsentStore.GetUserConsentAsync("test@test.com", "1"));

            await _userConsentStore.RemoveUserConsentAsync("test@test.com", "1");

            Assert.Null(await _userConsentStore.GetUserConsentAsync("test@test.com", "1"));
        }

        [Fact(DisplayName = "StoreUserConsentAsync should return save consent")]
        public async Task StoreUserConsentAsync()
        {
            using (var session = _documentStore.OpenSession(database))
            {
                session.Store(new Consent { ClientId = "1" }, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes("1"))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes("test@test.com"))}");
                session.Store(new Consent { ClientId = "3" }, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes("3"))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes("test@test.com"))}");
                session.SaveChanges();
            }

            Assert.Null(await _userConsentStore.GetUserConsentAsync("test@test.com", "2"));

            await _userConsentStore.StoreUserConsentAsync(new Consent { ClientId = "2", CreationTime = DateTime.UtcNow, SubjectId = "test@test.com" });

            Assert.NotNull(await _userConsentStore.GetUserConsentAsync("test@test.com", "2"));
        }

        [Fact(DisplayName = "Parameter validation should trigger argument exceptions")]
        public async Task Validation()
        {
            Assert.Throws<ArgumentException>(() => new RavenDBUserConsentStore(null, _documentStore, _identityStoreOptions));
            Assert.Throws<ArgumentException>(() => new RavenDBUserConsentStore(_loggerFactory.CreateLogger<RavenDBUserConsentStore>(), null, _identityStoreOptions));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _userConsentStore.GetUserConsentAsync(null, "client"));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _userConsentStore.GetUserConsentAsync("sub", null));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _userConsentStore.RemoveUserConsentAsync(null, "client"));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _userConsentStore.RemoveUserConsentAsync("sub", null));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _userConsentStore.StoreUserConsentAsync(null));
        }
    }
}
