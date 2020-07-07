using IdentityServer4.Contrib.RavenDB.Entities;
using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RavenDB.Stores
{
    public class RavenDBDeviceFlowStore : IDeviceFlowStore
    {
        private readonly ILogger _logger;
        private readonly IDocumentStore _store;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public RavenDBDeviceFlowStore(ILogger<RavenDBDeviceFlowStore> logger, IDocumentStore store, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _logger = logger ?? throw new ArgumentException("loggerFactory is required", nameof(logger));
            _store = store ?? throw new ArgumentException("store is required", nameof(store));
            _identityStoreOptions = identityStoreOptions;
        }

        public async Task<DeviceCode> FindByDeviceCodeAsync(string deviceCode)
        {
            if (string.IsNullOrEmpty(deviceCode))
                throw new ArgumentException("deviceCode is required", nameof(deviceCode));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                _logger.LogDebug($"Finding device code {deviceCode}");
                var entity = await session.Query<DeviceCodeEntity>().FirstOrDefaultAsync(t => t.DeviceCode.Equals(deviceCode)).ConfigureAwait(false);

                ClaimsPrincipal principal = null;
                if (entity.Principal != null)
                    principal = new ClaimsPrincipal(new ClaimsIdentity(entity.Principal.Claims.Select(x => new Claim(x.Type, x.Value, x.ValueType)), entity.Principal.AuthenticationType));

                return new DeviceCode
                {
                    AuthorizedScopes = entity.AuthorizedScopes,
                    ClientId = entity.ClientId,
                    CreationTime = entity.CreationTime,
                    IsAuthorized = entity.IsAuthorized,
                    IsOpenId = entity.IsOpenId,
                    Lifetime = entity.Lifetime,
                    RequestedScopes = entity.RequestedScopes,
                    Subject = principal
                };
            }
        }

        public async Task<DeviceCode> FindByUserCodeAsync(string userCode)
        {
            if (string.IsNullOrEmpty(userCode))
                throw new ArgumentException("userCode is required", nameof(userCode));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                _logger.LogDebug($"Loading device code with user code {userCode}");
                var entity = await session.LoadAsync<DeviceCodeEntity>($"DeviceCodes/{userCode}").ConfigureAwait(false);
                if (entity != null)
                {
                    ClaimsPrincipal principal = null;
                    if (entity.Principal != null)
                        principal = new ClaimsPrincipal(new ClaimsIdentity(entity.Principal.Claims.Select(x => new Claim(x.Type, x.Value, x.ValueType)), entity.Principal.AuthenticationType));

                    return new DeviceCode
                    {
                        AuthorizedScopes = entity.AuthorizedScopes,
                        ClientId = entity.ClientId,
                        CreationTime = entity.CreationTime,
                        IsAuthorized = entity.IsAuthorized,
                        IsOpenId = entity.IsOpenId,
                        Lifetime = entity.Lifetime,
                        RequestedScopes = entity.RequestedScopes,
                        Subject = principal
                    };
                }
                return null;
            }
        }

        public async Task RemoveByDeviceCodeAsync(string deviceCode)
        {
            if (string.IsNullOrEmpty(deviceCode))
                throw new ArgumentException("Device code is required", nameof(deviceCode));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                var code = await session.Query<DeviceCodeEntity>().FirstOrDefaultAsync(t => t.DeviceCode.Equals(deviceCode)).ConfigureAwait(false);
                if (code == null)
                    throw new KeyNotFoundException($"Device code {deviceCode} was not found");

                _logger.LogDebug($"Deleting device code {deviceCode}");
                session.Delete(code);
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task StoreDeviceAuthorizationAsync(string deviceCode, string userCode, DeviceCode data)
        {
            if (string.IsNullOrEmpty(deviceCode))
                throw new ArgumentException("deviceCode is required", nameof(deviceCode));

            if (string.IsNullOrEmpty(userCode))
                throw new ArgumentException("userCode is required", nameof(userCode));

            if (data == null)
                throw new ArgumentException("data is required", nameof(data));

            _logger.LogDebug($"Storing device code with user code {userCode}");
            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                var code = new DeviceCodeEntity
                {
                    AuthorizedScopes = data.AuthorizedScopes,
                    ClientId = data.ClientId,
                    CreationTime = data.CreationTime,
                    DeviceCode = deviceCode,
                    Id = $"DeviceCodes/{userCode}",
                    IsAuthorized = data.IsAuthorized,
                    IsOpenId = data.IsOpenId,
                    Lifetime = data.Lifetime,
                    RequestedScopes = data.RequestedScopes
                };
                if (data.Subject?.Identity != null)
                    code.Principal = new ClaimsPrincipalLite
                    {
                        AuthenticationType = data.Subject.Identity.AuthenticationType,
                        Claims = data.Subject?.Claims.Select(x => new ClaimLite { Type = x.Type, Value = x.Value, ValueType = x.ValueType }).ToArray()
                    };
                await session.StoreAsync(code).ConfigureAwait(false);
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task UpdateByUserCodeAsync(string userCode, DeviceCode data)
        {
            if (string.IsNullOrEmpty(userCode))
                throw new ArgumentException("userCode is required", nameof(userCode));

            if (data == null)
                throw new ArgumentException("data is required", nameof(data));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                var code = await session.LoadAsync<DeviceCodeEntity>($"DeviceCodes/{userCode}").ConfigureAwait(false);
                if (code == null)
                    throw new KeyNotFoundException($"Device code with UserCode {userCode} was not found");

                _logger.LogDebug($"Updating device flow {userCode}");

                code.AuthorizedScopes = data.AuthorizedScopes;
                code.ClientId = data.ClientId;
                code.CreationTime = data.CreationTime;
                code.IsAuthorized = data.IsAuthorized;
                code.IsOpenId = data.IsOpenId;
                code.Lifetime = data.Lifetime;
                code.RequestedScopes = data.RequestedScopes;
                if (data.Subject?.Identity != null)
                    code.Principal = new ClaimsPrincipalLite
                    {
                        AuthenticationType = data.Subject.Identity.AuthenticationType,
                        Claims = data.Subject?.Claims.Select(x => new ClaimLite { Type = x.Type, Value = x.Value, ValueType = x.ValueType }).ToArray()
                    };

                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}
