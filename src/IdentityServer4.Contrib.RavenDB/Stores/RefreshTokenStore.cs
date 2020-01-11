using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using shortid;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RavenDB.Stores
{
    public class RefreshTokenStore : IRefreshTokenStore
    {
        private readonly ILogger _logger;
        private readonly IDocumentStore _store;

        public RefreshTokenStore(ILoggerFactory loggerFactory, IDocumentStore store)
        {
            _logger = loggerFactory.CreateLogger<RefreshTokenStore>();
            _store = store;
        }

        public async Task<RefreshToken> GetRefreshTokenAsync(string refreshTokenHandle)
        {
            if (string.IsNullOrEmpty(refreshTokenHandle))
                throw new ArgumentException("refreshTokenHandle is required", nameof(refreshTokenHandle));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Loading refresh token {refreshTokenHandle}");
                return await session.LoadAsync<RefreshToken>($"RefreshTokens/{refreshTokenHandle}");
            }
        }

        public async Task RemoveRefreshTokenAsync(string refreshTokenHandle)
        {
            if (string.IsNullOrEmpty(refreshTokenHandle))
                throw new ArgumentException("refreshTokenHandle is required", nameof(refreshTokenHandle));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Deleting refresh token {refreshTokenHandle}");
                session.Delete($"RefreshTokens/{refreshTokenHandle}");
                await session.SaveChangesAsync();
            }
        }

        public async Task RemoveRefreshTokensAsync(string subjectId, string clientId)
        {
            if (string.IsNullOrEmpty(subjectId))
                throw new ArgumentException("subjectId is required", nameof(subjectId));

            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("clientId is required", nameof(clientId));

            using (var session = _store.OpenAsyncSession())
            {
                var token = await session.Query<RefreshToken>().FirstOrDefaultAsync(t => t.SubjectId.Equals(subjectId) && t.ClientId.Equals(clientId));
                if (token == null)
                    throw new KeyNotFoundException($"Refresh token with subjectId {subjectId} and clientId {clientId} was not found");

                _logger.LogDebug($"Deleting refresh token with subjectId {subjectId} and clientId {clientId}");
                session.Delete(token);
                await session.SaveChangesAsync();
            }
        }

        public async Task<string> StoreRefreshTokenAsync(RefreshToken refreshToken)
        {
            if (refreshToken == null)
                throw new ArgumentException("refreshToken is required", nameof(refreshToken));

            using (var session = _store.OpenAsyncSession())
            {
                var newToken = ShortId.Generate(true, false, 14);

                _logger.LogDebug($"Storing refresh token {newToken}");
                await session.StoreAsync(refreshToken, $"RefreshTokens/{newToken}");
                await session.SaveChangesAsync();

                return newToken;
            }
        }

        public async Task UpdateRefreshTokenAsync(string handle, RefreshToken refreshToken)
        {
            if (string.IsNullOrEmpty(handle))
                throw new ArgumentException("handle is required", nameof(handle));

            if (refreshToken == null)
                throw new ArgumentException("refreshToken is required", nameof(refreshToken));

            using (var session = _store.OpenAsyncSession())
            {
                var token = await session.LoadAsync<RefreshToken>($"RefreshTokens/{handle}");
                if (token == null)
                    throw new KeyNotFoundException($"Refresh token with handle {handle} was not found");

                _logger.LogDebug($"Updating refresh token {handle}");
                token.AccessToken = refreshToken.AccessToken;
                token.CreationTime = refreshToken.CreationTime;
                token.Lifetime = refreshToken.Lifetime;
                token.Version++;

                await session.SaveChangesAsync();
            }
        }
    }
}
