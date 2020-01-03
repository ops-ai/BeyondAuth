using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RavenDB.Stores
{
    public class PersistedGrantStore : IPersistedGrantStore
    {
        private readonly ILogger _logger;
        private readonly IPersistentGrantSerializer _serializer;
        private readonly IDocumentStore _store;

        public PersistedGrantStore(IPersistentGrantSerializer serializer, ILoggerFactory loggerFactory, IDocumentStore store)
        {
            _serializer = serializer;
            _logger = loggerFactory.CreateLogger<PersistedGrantStore>();
            _store = store;
        }

        public Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            throw new NotImplementedException();
        }

        public Task<PersistedGrant> GetAsync(string key)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAllAsync(string subjectId, string clientId)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(string key)
        {
            throw new NotImplementedException();
        }

        public Task StoreAsync(PersistedGrant grant)
        {
            throw new NotImplementedException();
        }
    }
}
