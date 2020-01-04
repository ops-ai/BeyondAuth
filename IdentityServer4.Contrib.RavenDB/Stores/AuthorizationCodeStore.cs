using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using shortid;
using System;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RavenDB.Stores
{
    public class AuthorizationCodeStore : IAuthorizationCodeStore
    {
        private readonly ILogger _logger;
        private readonly IDocumentStore _store;

        public AuthorizationCodeStore(ILoggerFactory loggerFactory, IDocumentStore store)
        {
            _logger = loggerFactory.CreateLogger<AuthorizationCodeStore>();
            _store = store;
        }

        public async Task<AuthorizationCode> GetAuthorizationCodeAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
                throw new ArgumentException("code is required", nameof(code));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Loading authorization code {code} from document store");
                return await session.LoadAsync<AuthorizationCode>($"AuthorizationCodes/{code}");
            }
        }

        public async Task RemoveAuthorizationCodeAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
                throw new ArgumentException("code is required", nameof(code));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Deleting authorization code {code} from document store");
                session.Delete($"AuthorizationCodes/{code}");
                await session.SaveChangesAsync();
            }
        }

        public async Task<string> StoreAuthorizationCodeAsync(AuthorizationCode code)
        {
            if (code == null)
                throw new ArgumentException("code is required", nameof(code));

            using (var session = _store.OpenAsyncSession())
            {
                var newCode = ShortId.Generate(true, false, 14);
                _logger.LogDebug($"Storing authorization code {code} into document store");
                await session.StoreAsync(code, $"AuthorizationCodes/{newCode}");
                await session.SaveChangesAsync();
                return newCode;
            }
        }
    }
}
