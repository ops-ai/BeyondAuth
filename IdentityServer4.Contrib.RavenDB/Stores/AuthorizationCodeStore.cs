using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
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

        public Task<AuthorizationCode> GetAuthorizationCodeAsync(string code)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAuthorizationCodeAsync(string code)
        {
            throw new NotImplementedException();
        }

        public Task<string> StoreAuthorizationCodeAsync(AuthorizationCode code)
        {
            throw new NotImplementedException();
        }
    }
}
