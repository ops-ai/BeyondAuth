using Divergic.Logging.Xunit;
using IdentityServer4.Contrib.RavenDB.Stores;
using IdentityServer4.Contrib.RavenDB.Tests.Common;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IdentityServer4.Contrib.RavenDB.Tests
{
    public class RavenDBResourceStoreTests : RavenIdentityServerTestBase
    {
        protected readonly ILoggerFactory _loggerFactory;
        protected readonly IDocumentStore _documentStore;
        private readonly IResourceStore _resourceStore;

        public RavenDBResourceStoreTests(ITestOutputHelper output)
        {
            _loggerFactory = LogFactory.Create(output);
            _documentStore = GetDocumentStore();
            _resourceStore = new RavenDBResourceStore(_loggerFactory.CreateLogger<RavenDBResourceStore>(), _documentStore);
        }

        [Fact(DisplayName = "FindApiResourceAsync should return resource")]
        public async Task FindApiResourceAsync()
        {
            using (var session = _documentStore.OpenSession())
            {
                session.Store(new ApiResource { DisplayName = "test", Scopes = new[] { "testscope" }, Name = "test" }, "ApiResources/test");
                session.Store(new ApiResource { DisplayName = "test2", Scopes = new [] { "scope2" }, Name = "test2" }, "ApiResources/test2");
                session.SaveChanges();
                WaitForIndexing(_documentStore);
            }

            var resource = await _resourceStore.FindApiResourcesByNameAsync(new[] { "test" });

            Assert.NotNull(resource.FirstOrDefault());
            Assert.Equal("test", resource.FirstOrDefault().Name);
            Assert.Equal("test", resource.FirstOrDefault().DisplayName);
        }

        [Fact(DisplayName = "FindApiResourceAsync should return null when resource doesn't exist")]
        public async Task FindApiResourceAsyncNull()
        {
            using (var session = _documentStore.OpenSession())
            {
                session.Store(new ApiResource { DisplayName = "test", Scopes = new [] { "testscope" }, Name = "test" }, "ApiResources/test");
                session.Store(new ApiResource { DisplayName = "test2", Scopes = new [] { "scope2" }, Name = "test2" }, "ApiResources/test2");
                session.SaveChanges();
                WaitForIndexing(_documentStore);
            }

            var resource = await _resourceStore.FindApiResourcesByNameAsync(new[] { "test3" });

            Assert.False(resource.Any());
        }

        [Fact(DisplayName = "FindApiResourcesByScopeAsync should return resource")]
        public async Task FindApiResourcesByScopeAsync()
        {
            using (var session = _documentStore.OpenSession())
            {
                session.Store(new ApiResource { DisplayName = "test", Scopes = new [] { "testscope" }, Name = "test" }, "ApiResources/test");
                session.Store(new ApiResource { DisplayName = "test2", Scopes = new [] { "scope2" }, Name = "test2" }, "ApiResources/test2");
                session.Store(new ApiResource { DisplayName = "test3", Scopes = new [] { "scope2" }, Name = "test3" }, "ApiResources/test3");
                session.SaveChanges();
                WaitForIndexing(_documentStore);
            }

            var resources = (await _resourceStore.FindApiResourcesByScopeNameAsync(new [] { "scope2" })).ToList();

            Assert.NotEmpty(resources);
            Assert.Equal(2, resources.Count);
            Assert.Equal("test2", resources[0].Name);
            Assert.Equal("test2", resources[0].DisplayName);
            Assert.Equal("test3", resources[1].Name);
            Assert.Equal("test3", resources[1].DisplayName);
        }

        [Fact(DisplayName = "FindApiResourcesByScopeAsync should return null when resource doesn't exist")]
        public async Task FindApiResourcesByScopeAsyncNull()
        {
            using (var session = _documentStore.OpenSession())
            {
                session.Store(new ApiResource { DisplayName = "test", Scopes = new [] { "testscope" }, Name = "test" }, "ApiResources/test");
                session.Store(new ApiResource { DisplayName = "test2", Scopes = new [] { "scope2" }, Name = "test2" }, "ApiResources/test2");
                session.Store(new ApiResource { DisplayName = "test3", Scopes = new [] { "scope2" }, Name = "test3" }, "ApiResources/test3");
                session.SaveChanges();
                WaitForIndexing(_documentStore);
            }

            var resources = (await _resourceStore.FindApiResourcesByScopeNameAsync(new List<string> { "scope3" })).ToList();

            Assert.Empty(resources);
        }

        [Fact(DisplayName = "FindIdentityResourcesByScopeAsync should return resource")]
        public async Task FindIdentityResourcesByScopeAsync()
        {
            using (var session = _documentStore.OpenSession())
            {
                session.Store(new IdentityResource { DisplayName = "test", Name = "test" }, "IdentityResources/test");
                session.Store(new IdentityResource { DisplayName = "test2", Name = "test2" }, "IdentityResources/test2");
                session.Store(new IdentityResource { DisplayName = "test3", Name = "test3" }, "IdentityResources/test3");
                session.SaveChanges();
                WaitForIndexing(_documentStore);
            }

            var resources = (await _resourceStore.FindIdentityResourcesByScopeNameAsync(new List<string> { "test2", "test3" })).ToList();

            Assert.NotEmpty(resources);
            Assert.Equal(2, resources.Count);
            Assert.Equal("test2", resources[0].Name);
            Assert.Equal("test2", resources[0].DisplayName);
            Assert.Equal("test3", resources[1].Name);
            Assert.Equal("test3", resources[1].DisplayName);
        }

        [Fact(DisplayName = "FindIdentityResourcesByScopeAsync should return null when resource doesn't exist")]
        public async Task FindIdentityResourcesByScopeAsyncNull()
        {
            using (var session = _documentStore.OpenSession())
            {
                session.Store(new IdentityResource { DisplayName = "test", Name = "test" }, "IdentityResources/test");
                session.Store(new IdentityResource { DisplayName = "test2", Name = "test2" }, "IdentityResources/test2");
                session.SaveChanges();
                WaitForIndexing(_documentStore);
            }

            var resources = (await _resourceStore.FindIdentityResourcesByScopeNameAsync(new List<string> { "test3" })).ToList();

            Assert.Empty(resources);
        }

        [Fact(DisplayName = "GetAllResourcesAsync should return both identity and api resources")]
        public async Task GetAllResourcesAsync()
        {
            using (var session = _documentStore.OpenSession())
            {
                session.Store(new ApiResource { DisplayName = "test", Scopes = new [] { "testscope" }, Name = "test" }, "ApiResources/test");
                session.Store(new ApiResource { DisplayName = "test2", Scopes = new [] { "scope2" }, Name = "test2" }, "ApiResources/test2");
                session.Store(new ApiResource { DisplayName = "test3", Scopes = new [] { "scope2" }, Name = "test3" }, "ApiResources/test3");
                session.Store(new IdentityResource { DisplayName = "test", Name = "test" }, "IdentityResources/test");
                session.Store(new IdentityResource { DisplayName = "test2", Name = "test2" }, "IdentityResources/test2");
                session.Store(new IdentityResource { DisplayName = "test3", Name = "test3" }, "IdentityResources/test3");
                session.SaveChanges();
                WaitForIndexing(_documentStore);
            }

            var resources = await _resourceStore.GetAllResourcesAsync();

            Assert.NotEmpty(resources.ApiResources);
            Assert.NotEmpty(resources.IdentityResources);
            Assert.Equal(3, resources.ApiResources.Count);
            Assert.Equal(3, resources.IdentityResources.Count);
            Assert.Equal("test", resources.ApiResources.ToList()[0].Name);
            Assert.Equal("test", resources.ApiResources.ToList()[0].DisplayName);
            Assert.Equal("test2", resources.ApiResources.ToList()[1].Name);
            Assert.Equal("test2", resources.ApiResources.ToList()[1].DisplayName);
            Assert.Equal("test", resources.IdentityResources.ToList()[0].Name);
            Assert.Equal("test", resources.IdentityResources.ToList()[0].DisplayName);
            Assert.Equal("test2", resources.IdentityResources.ToList()[1].Name);
            Assert.Equal("test2", resources.IdentityResources.ToList()[1].DisplayName);
        }

        [Fact(DisplayName = "Parameter validation should trigger argument exceptions")]
        public async Task Validation()
        {
            Assert.Throws<ArgumentException>(() => new RavenDBResourceStore(null, _documentStore));
            Assert.Throws<ArgumentException>(() => new RavenDBResourceStore(_loggerFactory.CreateLogger<RavenDBResourceStore>(), null));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _resourceStore.FindApiResourcesByNameAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _resourceStore.FindApiResourcesByScopeNameAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _resourceStore.FindEnabledResourcesByScopeAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _resourceStore.FindIdentityResourcesByScopeNameAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _resourceStore.FindEnabledIdentityResourcesByScopeAsync(null));
        }
    }
}
