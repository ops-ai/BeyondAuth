using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using shortid;
using shortid.Configuration;
using System;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RavenDB.Stores
{
    public class RavenDBAuthorizationCodeStore : IAuthorizationCodeStore
    {
        private readonly ILogger _logger;
        private readonly IDocumentStore _store;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public RavenDBAuthorizationCodeStore(ILogger<RavenDBAuthorizationCodeStore> logger, IDocumentStore store, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _logger = logger ?? throw new ArgumentException("loggerFactory is required", nameof(logger));
            _store = store ?? throw new ArgumentException("store is required", nameof(store));
            _identityStoreOptions = identityStoreOptions;
        }

        public async Task<AuthorizationCode> GetAuthorizationCodeAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
                throw new ArgumentException("code is required", nameof(code));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                _logger.LogDebug($"Loading authorization code {code}");
                return await session.LoadAsync<AuthorizationCode>($"AuthorizationCodes/{code}").ConfigureAwait(false);
            }
        }

        public async Task RemoveAuthorizationCodeAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
                throw new ArgumentException("code is required", nameof(code));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                _logger.LogDebug($"Deleting authorization code {code}");
                session.Delete($"AuthorizationCodes/{code}");
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task<string> StoreAuthorizationCodeAsync(AuthorizationCode code)
        {
            if (code == null)
                throw new ArgumentException("code is required", nameof(code));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                var newCode = ShortId.Generate(new GenerationOptions { Length = 14, UseNumbers = true, UseSpecialCharacters = false });
                _logger.LogDebug($"Storing authorization code {code}");
                await session.StoreAsync(code, $"AuthorizationCodes/{newCode}").ConfigureAwait(false);
                await session.SaveChangesAsync().ConfigureAwait(false);
                return newCode;
            }
        }
    }
}
