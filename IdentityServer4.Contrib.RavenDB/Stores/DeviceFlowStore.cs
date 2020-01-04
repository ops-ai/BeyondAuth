﻿using IdentityServer4.Contrib.RavenDB.Entities;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RavenDB.Stores
{
    public class DeviceFlowStore : IDeviceFlowStore
    {
        private readonly ILogger _logger;
        private readonly IDocumentStore _store;

        public DeviceFlowStore(ILoggerFactory loggerFactory, IDocumentStore store)
        {
            _logger = loggerFactory.CreateLogger<DeviceFlowStore>();
            _store = store;
        }

        public async Task<DeviceCode> FindByDeviceCodeAsync(string deviceCode)
        {
            if (string.IsNullOrEmpty(deviceCode))
                throw new ArgumentException("deviceCode is required", nameof(deviceCode));

            using (var session = _store.OpenAsyncSession())
            {
                return await session.Query<DeviceCodeEntity>().FirstOrDefaultAsync(t => t.DeviceCode.Equals(deviceCode));
            }
        }

        public async Task<DeviceCode> FindByUserCodeAsync(string userCode)
        {
            if (string.IsNullOrEmpty(userCode))
                throw new ArgumentException("userCode is required", nameof(userCode));

            using (var session = _store.OpenAsyncSession())
            {
                return await session.LoadAsync<DeviceCodeEntity>($"DeviceCodes/{userCode}");
            }
        }

        public async Task RemoveByDeviceCodeAsync(string deviceCode)
        {
            if (string.IsNullOrEmpty(deviceCode))
                throw new ArgumentException("Device code is required", nameof(deviceCode));

            using (var session = _store.OpenAsyncSession())
            {
                var code = await session.Query<DeviceCodeEntity>().FirstOrDefaultAsync(t => t.DeviceCode.Equals(deviceCode));
                session.Delete(code);
                await session.SaveChangesAsync();
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

            using (var session = _store.OpenAsyncSession())
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
                    RequestedScopes = data.RequestedScopes,
                    Subject = data.Subject
                };
                await session.StoreAsync(code);
                await session.SaveChangesAsync();
            }
        }

        public async Task UpdateByUserCodeAsync(string userCode, DeviceCode data)
        {
            if (string.IsNullOrEmpty(userCode))
                throw new ArgumentException("userCode is required", nameof(userCode));

            if (data == null)
                throw new ArgumentException("data is required", nameof(data));

            using (var session = _store.OpenAsyncSession())
            {
                var code = await session.LoadAsync<DeviceCodeEntity>($"DeviceCodes/{userCode}");
                if (code == null)
                    throw new KeyNotFoundException($"Device code with UserCode {userCode} was not found");

                code.AuthorizedScopes = data.AuthorizedScopes;
                code.ClientId = data.ClientId;
                code.CreationTime = data.CreationTime;
                code.IsAuthorized = data.IsAuthorized;
                code.IsOpenId = data.IsOpenId;
                code.Lifetime = data.Lifetime;
                code.RequestedScopes = data.RequestedScopes;
                code.Subject = data.Subject;

                await session.SaveChangesAsync();
            }
        }
    }
}
