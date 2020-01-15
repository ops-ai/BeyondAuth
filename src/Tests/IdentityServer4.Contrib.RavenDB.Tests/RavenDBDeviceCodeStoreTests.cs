using Divergic.Logging.Xunit;
using FluentAssertions;
using IdentityServer4.Contrib.RavenDB.Stores;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Raven.TestDriver;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IdentityServer4.Contrib.RavenDB.Tests
{
    public class RavenDBDeviceCodeStoreTests : RavenTestDriver
    {
        protected readonly ILoggerFactory _loggerFactory;
        protected readonly IDeviceFlowStore _deviceFlowStore;

        public RavenDBDeviceCodeStoreTests(ITestOutputHelper output)
        {
            _loggerFactory = LogFactory.Create(output);
            _deviceFlowStore = new RavenDBDeviceFlowStore(_loggerFactory, GetDocumentStore());
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
                Subject = null,
                RequestedScopes = new[] { "scope1", "scope2" }
            };

            await _deviceFlowStore.StoreDeviceAuthorizationAsync(deviceCode, userCode, data);
            var foundData = await _deviceFlowStore.FindByUserCodeAsync(userCode);

            foundData.ClientId.Should().Be(data.ClientId);
            foundData.CreationTime.Should().Be(data.CreationTime);
            foundData.Lifetime.Should().Be(data.Lifetime);
            foundData.IsAuthorized.Should().Be(data.IsAuthorized);
            foundData.IsOpenId.Should().Be(data.IsOpenId);
            foundData.Subject.Should().Be(data.Subject);
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
        }
    }
}
