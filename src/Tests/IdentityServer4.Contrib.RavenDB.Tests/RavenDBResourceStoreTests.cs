using Divergic.Logging.Xunit;
using IdentityServer4.Contrib.RavenDB.Stores;
using IdentityServer4.Models;
using Microsoft.Extensions.Logging;
using Raven.TestDriver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IdentityServer4.Contrib.RavenDB.Tests
{
    public class RavenDBResourceStoreTests : RavenTestDriver
    {
        protected readonly ILoggerFactory _loggerFactory;

        public RavenDBResourceStoreTests(ITestOutputHelper output)
        {
            _loggerFactory = LogFactory.Create(output);
        }

        [Fact(DisplayName = "FindApiResourceAsync should return resource")]
        public async Task InMemoryResourcesStore_should_not_throw_if_does_not_contains_duplicate_names()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new ApiResource { DisplayName = "test", Scopes = new List<Scope> { new Scope { Name = "testscope" } }, Name = "test" }, "ApiResources/test");
                    session.Store(new ApiResource { DisplayName = "test2", Scopes = new List<Scope> { new Scope { Name = "scope2" } }, Name = "test2" }, "ApiResources/test2");
                    session.SaveChanges();
                    WaitForIndexing(store);
                }

                var resourceStore = new RavenDBResourceStore(_loggerFactory.CreateLogger<RavenDBResourceStore>(), store);
                var resource = await resourceStore.FindApiResourceAsync("test");
                
                Assert.NotNull(resource);
                Assert.Equal("test", resource.Name);
                Assert.Equal("test", resource.DisplayName);
            }
        }

        [Fact(DisplayName = "FindApiResourceAsync should return null when resource doesn't exist")]
        public async Task FindApiResourceAsyncNull()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new ApiResource { DisplayName = "test", Scopes = new List<Scope> { new Scope { Name = "testscope" } }, Name = "test" }, "ApiResources/test");
                    session.Store(new ApiResource { DisplayName = "test2", Scopes = new List<Scope> { new Scope { Name = "scope2" } }, Name = "test2" }, "ApiResources/test2");
                    session.SaveChanges();
                    WaitForIndexing(store);
                }

                var resourceStore = new RavenDBResourceStore(_loggerFactory.CreateLogger<RavenDBResourceStore>(), store);
                var resource = await resourceStore.FindApiResourceAsync("test3");

                Assert.Null(resource);
            }
        }

        [Fact(DisplayName = "FindApiResourcesByScopeAsync should return resource")]
        public async Task FindApiResourcesByScopeAsync()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new ApiResource { DisplayName = "test", Scopes = new List<Scope> { new Scope { Name = "testscope" } }, Name = "test" }, "ApiResources/test");
                    session.Store(new ApiResource { DisplayName = "test2", Scopes = new List<Scope> { new Scope { Name = "scope2" } }, Name = "test2" }, "ApiResources/test2");
                    session.Store(new ApiResource { DisplayName = "test3", Scopes = new List<Scope> { new Scope { Name = "scope2" } }, Name = "test3" }, "ApiResources/test3");
                    session.SaveChanges();
                    WaitForIndexing(store);
                }

                var resourceStore = new RavenDBResourceStore(_loggerFactory.CreateLogger<RavenDBResourceStore>(), store);
                var resources = (await resourceStore.FindApiResourcesByScopeAsync(new List<string> { "scope2" })).ToList();

                Assert.NotEmpty(resources);
                Assert.Equal(2, resources.Count);
                Assert.Equal("test2", resources[0].Name);
                Assert.Equal("test2", resources[0].DisplayName);
                Assert.Equal("test3", resources[1].Name);
                Assert.Equal("test3", resources[1].DisplayName);
            }
        }

        [Fact(DisplayName = "FindApiResourcesByScopeAsync should return null when resource doesn't exist")]
        public async Task FindApiResourcesByScopeAsyncNull()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new ApiResource { DisplayName = "test", Scopes = new List<Scope> { new Scope { Name = "testscope" } }, Name = "test" }, "ApiResources/test");
                    session.Store(new ApiResource { DisplayName = "test2", Scopes = new List<Scope> { new Scope { Name = "scope2" } }, Name = "test2" }, "ApiResources/test2");
                    session.Store(new ApiResource { DisplayName = "test3", Scopes = new List<Scope> { new Scope { Name = "scope2" } }, Name = "test3" }, "ApiResources/test3");
                    session.SaveChanges();
                    WaitForIndexing(store);
                }

                var resourceStore = new RavenDBResourceStore(_loggerFactory.CreateLogger<RavenDBResourceStore>(), store);
                var resources = (await resourceStore.FindApiResourcesByScopeAsync(new List<string> { "scope3" })).ToList();

                Assert.Empty(resources);
            }
        }

        [Fact(DisplayName = "FindIdentityResourcesByScopeAsync should return resource")]
        public async Task FindIdentityResourcesByScopeAsync()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new IdentityResource { DisplayName = "test", Name = "test" }, "IdentityResources/test");
                    session.Store(new IdentityResource { DisplayName = "test2", Name = "test2" }, "IdentityResources/test2");
                    session.Store(new IdentityResource { DisplayName = "test3", Name = "test3" }, "IdentityResources/test3");
                    session.SaveChanges();
                    WaitForIndexing(store);
                }

                var resourceStore = new RavenDBResourceStore(_loggerFactory.CreateLogger<RavenDBResourceStore>(), store);
                var resources = (await resourceStore.FindIdentityResourcesByScopeAsync(new List<string> { "test2", "test3" })).ToList();

                Assert.NotEmpty(resources);
                Assert.Equal(2, resources.Count);
                Assert.Equal("test2", resources[0].Name);
                Assert.Equal("test2", resources[0].DisplayName);
                Assert.Equal("test3", resources[1].Name);
                Assert.Equal("test3", resources[1].DisplayName);
            }
        }

        [Fact(DisplayName = "FindIdentityResourcesByScopeAsync should return null when resource doesn't exist")]
        public async Task FindIdentityResourcesByScopeAsyncNull()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new IdentityResource { DisplayName = "test", Name = "test" }, "IdentityResources/test");
                    session.Store(new IdentityResource { DisplayName = "test2", Name = "test2" }, "IdentityResources/test2");
                    session.SaveChanges();
                    WaitForIndexing(store);
                }

                var resourceStore = new RavenDBResourceStore(_loggerFactory.CreateLogger<RavenDBResourceStore>(), store);
                var resources = (await resourceStore.FindIdentityResourcesByScopeAsync(new List<string> { "test3" })).ToList();

                Assert.Empty(resources);
            }
        }

        [Fact(DisplayName = "GetAllResourcesAsync should return both identity and api resources")]
        public async Task GetAllResourcesAsync()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new ApiResource { DisplayName = "test", Scopes = new List<Scope> { new Scope { Name = "testscope" } }, Name = "test" }, "ApiResources/test");
                    session.Store(new ApiResource { DisplayName = "test2", Scopes = new List<Scope> { new Scope { Name = "scope2" } }, Name = "test2" }, "ApiResources/test2");
                    session.Store(new ApiResource { DisplayName = "test3", Scopes = new List<Scope> { new Scope { Name = "scope2" } }, Name = "test3" }, "ApiResources/test3");
                    session.Store(new IdentityResource { DisplayName = "test", Name = "test" }, "IdentityResources/test");
                    session.Store(new IdentityResource { DisplayName = "test2", Name = "test2" }, "IdentityResources/test2");
                    session.Store(new IdentityResource { DisplayName = "test3", Name = "test3" }, "IdentityResources/test3");
                    session.SaveChanges();
                    WaitForIndexing(store);
                }

                var resourceStore = new RavenDBResourceStore(_loggerFactory.CreateLogger<RavenDBResourceStore>(), store);
                var resources = await resourceStore.GetAllResourcesAsync();

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
        }
    }
}
