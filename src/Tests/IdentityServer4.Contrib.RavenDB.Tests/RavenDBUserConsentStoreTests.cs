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
    public class RavenDBUserConsentStoreTests : RavenTestDriver
    {
        protected readonly ILoggerFactory _loggerFactory;

        public RavenDBUserConsentStoreTests(ITestOutputHelper output)
        {
            _loggerFactory = LogFactory.Create(output);
        }

        [Fact(DisplayName = "GetUserConsentAsync should return consent")]
        public async Task GetUserConsentAsync()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new Consent { ClientId = "1", CreationTime = DateTime.UtcNow, SubjectId = "test@test.com" }, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes("1"))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes("test@test.com"))}");
                    session.Store(new Consent { ClientId = "3", CreationTime = DateTime.UtcNow, SubjectId = "test@test.com" }, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes("3"))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes("test@test.com"))}");
                    session.SaveChanges();
                }

                var consentStore = new RavenDBUserConsentStore(_loggerFactory.CreateLogger<RavenDBUserConsentStore>(), store);
                Assert.NotNull(await consentStore.GetUserConsentAsync("test@test.com", "1"));
            }
        }

        [Fact(DisplayName = "GetUserConsentAsync should return null when consent doesn't exist")]
        public async Task RavenDBconsentStoreNoClient()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new Consent { ClientId = "1", CreationTime = DateTime.UtcNow, SubjectId = "test@test.com" }, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes("1"))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes("test@test.com"))}");
                    session.Store(new Consent { ClientId = "3", CreationTime = DateTime.UtcNow, SubjectId = "test@test.com" }, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes("3"))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes("test@test.com"))}");
                    session.SaveChanges();
                }

                var consentStore = new RavenDBUserConsentStore(_loggerFactory.CreateLogger<RavenDBUserConsentStore>(), store);
                Assert.Null(await consentStore.GetUserConsentAsync("nothing@test.com", "1"));
                Assert.Null(await consentStore.GetUserConsentAsync("test@test.com", "2"));
            }
        }

        [Fact(DisplayName = "RemoveUserConsentAsync should return remove consent")]
        public async Task RemoveUserConsentAsync()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new Consent { ClientId = "1", CreationTime = DateTime.UtcNow, SubjectId = "test@test.com" }, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes("1"))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes("test@test.com"))}");
                    session.Store(new Consent { ClientId = "3", CreationTime = DateTime.UtcNow, SubjectId = "test@test.com" }, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes("3"))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes("test@test.com"))}");
                    session.SaveChanges();
                }

                var consentStore = new RavenDBUserConsentStore(_loggerFactory.CreateLogger<RavenDBUserConsentStore>(), store);
                Assert.NotNull(await consentStore.GetUserConsentAsync("test@test.com", "1"));

                await consentStore.RemoveUserConsentAsync("test@test.com", "1");

                Assert.Null(await consentStore.GetUserConsentAsync("test@test.com", "1"));
            }
        }

        [Fact(DisplayName = "StoreUserConsentAsync should return save consent")]
        public async Task StoreUserConsentAsync()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new Consent { ClientId = "1" }, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes("1"))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes("test@test.com"))}");
                    session.Store(new Consent { ClientId = "3" }, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes("3"))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes("test@test.com"))}");
                    session.SaveChanges();
                }

                var consentStore = new RavenDBUserConsentStore(_loggerFactory.CreateLogger<RavenDBUserConsentStore>(), store);
                Assert.Null(await consentStore.GetUserConsentAsync("test@test.com", "2"));

                await consentStore.StoreUserConsentAsync(new Consent { ClientId = "2", CreationTime = DateTime.UtcNow, SubjectId = "test@test.com" });

                Assert.NotNull(await consentStore.GetUserConsentAsync("test@test.com", "2"));
            }
        }
    }
}
