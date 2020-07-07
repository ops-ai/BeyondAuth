using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using System;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RavenDB.Stores
{
    public class RavenDBUserConsentStore : IUserConsentStore
    {
        private readonly ILogger _logger;
        private readonly IDocumentStore _store;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public RavenDBUserConsentStore(ILogger<RavenDBUserConsentStore> logger, IDocumentStore store, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _logger = logger ?? throw new ArgumentException("loggerFactory is required", nameof(logger));
            _store = store ?? throw new ArgumentException("store is required", nameof(store));
            _identityStoreOptions = identityStoreOptions;
        }

        public async Task<Consent> GetUserConsentAsync(string subjectId, string clientId)
        {
            if (string.IsNullOrEmpty(subjectId))
                throw new ArgumentException("subjectId is required", nameof(subjectId));

            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("clientId is required", nameof(clientId));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                return await session.LoadAsync<Consent>($"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes(clientId))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes(subjectId))}").ConfigureAwait(false);
            }
        }

        public async Task RemoveUserConsentAsync(string subjectId, string clientId)
        {
            if (string.IsNullOrEmpty(subjectId))
                throw new ArgumentException("subjectId is required", nameof(subjectId));

            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("clientId is required", nameof(clientId));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                session.Delete($"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes(clientId))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes(subjectId))}");
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task StoreUserConsentAsync(Consent consent)
        {
            if (consent == null)
                throw new ArgumentException("consent is required", nameof(consent));

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                _logger.LogDebug($"Storing consent for clientId {consent.ClientId} and subjectId {consent.SubjectId}");
                await session.StoreAsync(consent, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes(consent.ClientId))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes(consent.SubjectId))}").ConfigureAwait(false);
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}
