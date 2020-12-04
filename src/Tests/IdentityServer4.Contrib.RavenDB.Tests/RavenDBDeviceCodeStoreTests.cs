using Divergic.Logging.Xunit;
using FluentAssertions;
using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Contrib.RavenDB.Stores;
using IdentityServer4.Contrib.RavenDB.Tests.Common;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.ServerWide.Operations;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IdentityServer4.Contrib.RavenDB.Tests
{
    [Collection("IdentityServer4 Tests")]
    public class RavenDBDeviceCodeStoreTests : RavenIdentityServerTestBase
    {
        protected readonly ILoggerFactory _loggerFactory;
        protected readonly IDeviceFlowStore _deviceFlowStore;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;
        private string database = Guid.NewGuid().ToString();

        public RavenDBDeviceCodeStoreTests(ITestOutputHelper output)
        {
            _loggerFactory = LogFactory.Create(output);
            var documentStore = GetDocumentStore();
            documentStore.EnsureDatabaseExists(database);
            _identityStoreOptions = Microsoft.Extensions.Options.Options.Create(new IdentityStoreOptions { DatabaseName = database });

            _deviceFlowStore = new RavenDBDeviceFlowStore(_loggerFactory.CreateLogger<RavenDBDeviceFlowStore>(), documentStore, _identityStoreOptions);
        }

        [Fact(DisplayName = "StoreDeviceAuthorizationAsync should persist data by user code")]
        public async Task PersistByUserCode()
        {
            var deviceCode = Guid.NewGuid().ToString();
            var userCode = Guid.NewGuid().ToString();
            var data = new DeviceCode
            {
                ClientId = Guid.NewGuid().ToString(),
                CreationTime = DateTime.UtcNow,
                Lifetime = 300,
                IsAuthorized = false,
                IsOpenId = true,
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("sub", "123") })),
                RequestedScopes = new[] { "scope1", "scope2" }
            };

            await _deviceFlowStore.StoreDeviceAuthorizationAsync(deviceCode, userCode, data);
            var foundData = await _deviceFlowStore.FindByUserCodeAsync(userCode);

            foundData.ClientId.Should().Be(data.ClientId);
            foundData.CreationTime.Should().Be(data.CreationTime);
            foundData.Lifetime.Should().Be(data.Lifetime);
            foundData.IsAuthorized.Should().Be(data.IsAuthorized);
            foundData.IsOpenId.Should().Be(data.IsOpenId);
            foundData.Subject.Claims.Should().BeEquivalentTo(data.Subject.Claims, x => x.IgnoringCyclicReferences());
            foundData.Subject.Identity.AuthenticationType.Should().BeEquivalentTo(data.Subject.Identity.AuthenticationType);
            foundData.Subject.Identity.Name.Should().BeEquivalentTo(data.Subject.Identity.Name);
            foundData.RequestedScopes.Should().BeEquivalentTo(data.RequestedScopes);
        }

        [Fact(DisplayName = "StoreDeviceAuthorizationAsync should persist data by device code")]
        public async Task PersistByDeviceCode()
        {
            var deviceCode = Guid.NewGuid().ToString();
            var userCode = Guid.NewGuid().ToString();
            var data = new DeviceCode
            {
                ClientId = Guid.NewGuid().ToString(),
                CreationTime = DateTime.UtcNow,
                Lifetime = 300,
                IsAuthorized = false,
                IsOpenId = true,
                Subject = null,
                RequestedScopes = new[] { "scope1", "scope2" }
            };

            await _deviceFlowStore.StoreDeviceAuthorizationAsync(deviceCode, userCode, data);
            var foundData = await _deviceFlowStore.FindByDeviceCodeAsync(deviceCode);

            foundData.ClientId.Should().Be(data.ClientId);
            foundData.CreationTime.Should().Be(data.CreationTime);
            foundData.Lifetime.Should().Be(data.Lifetime);
            foundData.IsAuthorized.Should().Be(data.IsAuthorized);
            foundData.IsOpenId.Should().Be(data.IsOpenId);
            foundData.Subject.Should().Be(data.Subject);
            foundData.RequestedScopes.Should().BeEquivalentTo(data.RequestedScopes);
        }

        [Fact(DisplayName = "StoreDeviceAuthorizationAsync should persist data by device code with no subject")]
        public async Task PersistByDeviceCodeNoSubject()
        {
            var deviceCode = Guid.NewGuid().ToString();
            var userCode = Guid.NewGuid().ToString();
            var data = new DeviceCode
            {
                ClientId = Guid.NewGuid().ToString(),
                CreationTime = DateTime.UtcNow,
                Lifetime = 300,
                IsAuthorized = false,
                IsOpenId = true,
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("sub", "123") })),
                RequestedScopes = new[] { "scope1", "scope2" }
            };

            await _deviceFlowStore.StoreDeviceAuthorizationAsync(deviceCode, userCode, data);
            var foundData = await _deviceFlowStore.FindByDeviceCodeAsync(deviceCode);

            foundData.ClientId.Should().Be(data.ClientId);
            foundData.CreationTime.Should().Be(data.CreationTime);
            foundData.Lifetime.Should().Be(data.Lifetime);
            foundData.IsAuthorized.Should().Be(data.IsAuthorized);
            foundData.IsOpenId.Should().Be(data.IsOpenId);
            foundData.Subject.Claims.Should().BeEquivalentTo(data.Subject.Claims, x => x.IgnoringCyclicReferences());
            foundData.Subject.Identity.AuthenticationType.Should().BeEquivalentTo(data.Subject.Identity.AuthenticationType);
            foundData.Subject.Identity.Name.Should().BeEquivalentTo(data.Subject.Identity.Name);
            foundData.RequestedScopes.Should().BeEquivalentTo(data.RequestedScopes);
        }

        [Fact(DisplayName = "UpdateByUserCodeAsync should update data")]
        public async Task UpdateData()
        {
            var deviceCode = Guid.NewGuid().ToString();
            var userCode = Guid.NewGuid().ToString();
            var initialData = new DeviceCode
            {
                ClientId = Guid.NewGuid().ToString(),
                CreationTime = DateTime.UtcNow,
                Lifetime = 300,
                IsAuthorized = false,
                IsOpenId = true,
                Subject = null,
                RequestedScopes = new[] { "scope1", "scope2" }
            };

            await _deviceFlowStore.StoreDeviceAuthorizationAsync(deviceCode, userCode, initialData);

            var updatedData = new DeviceCode
            {
                ClientId = Guid.NewGuid().ToString(),
                CreationTime = initialData.CreationTime.AddHours(2),
                Lifetime = initialData.Lifetime + 600,
                IsAuthorized = !initialData.IsAuthorized,
                IsOpenId = !initialData.IsOpenId,
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("sub", "123") })),
                RequestedScopes = new[] { "api1", "api2" }
            };

            await _deviceFlowStore.UpdateByUserCodeAsync(userCode, updatedData);

            var foundData = await _deviceFlowStore.FindByUserCodeAsync(userCode);

            foundData.ClientId.Should().Be(updatedData.ClientId);
            foundData.CreationTime.Should().Be(updatedData.CreationTime);
            foundData.Lifetime.Should().Be(updatedData.Lifetime);
            foundData.IsAuthorized.Should().Be(updatedData.IsAuthorized);
            foundData.IsOpenId.Should().Be(updatedData.IsOpenId);
            foundData.Subject.Claims.Should().BeEquivalentTo(updatedData.Subject.Claims, x => x.IgnoringCyclicReferences());
            foundData.Subject.Identity.AuthenticationType.Should().BeEquivalentTo(updatedData.Subject.Identity.AuthenticationType);
            foundData.Subject.Identity.Name.Should().BeEquivalentTo(updatedData.Subject.Identity.Name);
            foundData.RequestedScopes.Should().BeEquivalentTo(updatedData.RequestedScopes);

            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _deviceFlowStore.UpdateByUserCodeAsync("wrongcode", updatedData));
        }

        [Fact(DisplayName = "RemoveByDeviceCodeAsync should remove code")]
        public async Task RemoveCode()
        {
            var deviceCode = Guid.NewGuid().ToString();
            var userCode = Guid.NewGuid().ToString();
            var data = new DeviceCode
            {
                ClientId = Guid.NewGuid().ToString(),
                CreationTime = DateTime.UtcNow,
                Lifetime = 300,
                IsAuthorized = false,
                IsOpenId = true,
                Subject = null,
                RequestedScopes = new[] { "scope1", "scope2" }
            };

            await _deviceFlowStore.StoreDeviceAuthorizationAsync(deviceCode, userCode, data);
            await _deviceFlowStore.RemoveByDeviceCodeAsync(deviceCode);
            var foundData = await _deviceFlowStore.FindByUserCodeAsync(userCode);

            foundData.Should().BeNull();
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _deviceFlowStore.RemoveByDeviceCodeAsync(deviceCode));
        }

        [Fact(DisplayName = "Parameter validation should trigger argument exceptions")]
        public async Task Validation()
        {
            using (var store = GetDocumentStore())
            {
                var deviceFlowStore = new RavenDBDeviceFlowStore(_loggerFactory.CreateLogger<RavenDBDeviceFlowStore>(), store, _identityStoreOptions);

                Assert.Throws<ArgumentException>(() => new RavenDBDeviceFlowStore(null, store, _identityStoreOptions));
                Assert.Throws<ArgumentException>(() => new RavenDBDeviceFlowStore(_loggerFactory.CreateLogger<RavenDBDeviceFlowStore>(), null, _identityStoreOptions));
                await Assert.ThrowsAsync<ArgumentException>(async () => await deviceFlowStore.StoreDeviceAuthorizationAsync(null, "test", new DeviceCode { }));
                await Assert.ThrowsAsync<ArgumentException>(async () => await deviceFlowStore.StoreDeviceAuthorizationAsync("test", null, new DeviceCode { }));
                await Assert.ThrowsAsync<ArgumentException>(async () => await deviceFlowStore.StoreDeviceAuthorizationAsync("test", "test", null));
                await Assert.ThrowsAsync<ArgumentException>(async () => await deviceFlowStore.FindByUserCodeAsync(null));
                await Assert.ThrowsAsync<ArgumentException>(async () => await deviceFlowStore.FindByDeviceCodeAsync(null));
                await Assert.ThrowsAsync<ArgumentException>(async () => await deviceFlowStore.RemoveByDeviceCodeAsync(null));
                await Assert.ThrowsAsync<ArgumentException>(async () => await deviceFlowStore.StoreDeviceAuthorizationAsync(null, "code", new DeviceCode { }));
                await Assert.ThrowsAsync<ArgumentException>(async () => await deviceFlowStore.StoreDeviceAuthorizationAsync("code", null, new DeviceCode { }));
                await Assert.ThrowsAsync<ArgumentException>(async () => await deviceFlowStore.StoreDeviceAuthorizationAsync("code", "code", null));
                await Assert.ThrowsAsync<ArgumentException>(async () => await deviceFlowStore.UpdateByUserCodeAsync(null, new DeviceCode { }));
                await Assert.ThrowsAsync<ArgumentException>(async () => await deviceFlowStore.UpdateByUserCodeAsync("code", null));
            }
        }
    }
}
