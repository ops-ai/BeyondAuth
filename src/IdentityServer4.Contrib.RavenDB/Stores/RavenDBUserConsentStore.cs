using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
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

        public RavenDBUserConsentStore(ILoggerFactory loggerFactory, IDocumentStore store)
        {
            _logger = loggerFactory.CreateLogger<RavenDBUserConsentStore>();
            _store = store;
        }

        public async Task<Consent> GetUserConsentAsync(string subjectId, string clientId)
        {
            if (string.IsNullOrEmpty(subjectId))
                throw new ArgumentException("subjectId is required", nameof(subjectId));

            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("clientId is required", nameof(clientId));

            using (var session = _store.OpenAsyncSession())
            {
                return await session.LoadAsync<Consent>($"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes(clientId))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes(subjectId))}");
            }
        }

        public async Task RemoveUserConsentAsync(string subjectId, string clientId)
        {
            if (string.IsNullOrEmpty(subjectId))
                throw new ArgumentException("subjectId is required", nameof(subjectId));

            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("clientId is required", nameof(clientId));

            using (var session = _store.OpenAsyncSession())
            {
                session.Delete($"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes(clientId))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes(subjectId))}");
                await session.SaveChangesAsync();
            }
        }

        public async Task StoreUserConsentAsync(Consent consent)
        {
            if (consent == null)
                throw new ArgumentException("consent is required", nameof(consent));

            using (var session = _store.OpenAsyncSession())
            {
                _logger.LogDebug($"Storing consent for clientId {consent.ClientId} and subjectId {consent.SubjectId}");
                await session.StoreAsync(consent, $"Consents/{Convert.ToBase64String(Encoding.UTF8.GetBytes(consent.ClientId))}-{Convert.ToBase64String(Encoding.UTF8.GetBytes(consent.SubjectId))}");
                await session.SaveChangesAsync();
            }
        }
    }
}
