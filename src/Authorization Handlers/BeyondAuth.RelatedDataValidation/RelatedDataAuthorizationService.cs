using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Raven.Client.Documents;

namespace BeyondAuth.RelatedDataValidation
{
    public class RelatedDataAuthorizationService : IRelatedDataAuthorizationService
    {
        private readonly IDocumentStore _store;

        public RelatedDataAuthorizationService(IDocumentStore store)
        {
            _store = store;
        }

        public async Task AddResource(IRelatedDataEntity entity)
        {
            using (var session = _store.OpenAsyncSession())
            {
                if (!await ValidateResource(entity))
                    throw new ArgumentException($"Validation rule failed");

                await session.StoreAsync(entity, entity.Sha256HashCode);
                await session.SaveChangesAsync();
            }
        }

        public async Task AddResource(string hash, string relatedHash, Dictionary<string, List<string>> data)
        {
            using (var session = _store.OpenAsyncSession())
            {
                if (!await ValidateResource(hash, relatedHash, data))
                    throw new ArgumentException($"Validation rule failed");

                await session.StoreAsync(new RelatedDataEntity { Sha256HashCode = hash, RelSha256HashCode = relatedHash, Data = data }, hash);
                await session.SaveChangesAsync();
            }
        }

        public async Task<bool> ValidateResource(IRelatedDataEntity entity)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ValidateResource(string hash, string relatedHash, Dictionary<string, List<string>> data)
        {
            throw new NotImplementedException();
        }

        public async Task<RelatedDataEntity> GetResource(string hash)
        {
            using (var session = _store.OpenAsyncSession())
            {
                return await session.LoadAsync<RelatedDataEntity>(hash);
            }
        }

        public async Task AddValidationrule(RelatedDataValidationRule rule)
        {
            using (var session = _store.OpenAsyncSession())
            {
                await session.StoreAsync(rule);
                await session.SaveChangesAsync();
            }
        }

        public async Task<RelatedDataValidationRule> GetValidationRule(string id)
        {
            using (var session = _store.OpenAsyncSession())
            {
                return await session.LoadAsync<RelatedDataValidationRule>(id);
            }
        }

        public async Task<List<RelatedDataValidationRule>> GetValidationRules(Dictionary<string, List<string>> data)
        {
            using (var session = _store.OpenAsyncSession())
            {
                var propertyNames = data.Keys;
                
                // Find all the rules that could match
                var rules = session.Advanced.AsyncDocumentQuery<RelatedDataValidationRule>();
               


                return await rules.ToListAsync();
            }
        }
    }
}
