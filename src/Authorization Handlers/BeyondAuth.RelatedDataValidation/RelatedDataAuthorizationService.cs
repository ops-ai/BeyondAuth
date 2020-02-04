using BeyondAuth.RelatedDataValidation.Indices;
using BeyondAuth.RelatedDataValidation.Requirements;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeyondAuth.RelatedDataValidation
{
    public class RelatedDataAuthorizationService : IRelatedDataAuthorizationService
    {
        private readonly IDocumentStore _store;

        public RelatedDataAuthorizationService(IDocumentStore store) => _store = store;

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

        public async Task AddResource(string hash, string relatedHash, Dictionary<string, HashSet<string>> data)
        {
            using (var session = _store.OpenAsyncSession())
            {
                if (!await ValidateResource(hash, relatedHash, data))
                    throw new ArgumentException($"Validation rule failed");

                await session.StoreAsync(new RelatedDataEntity { Sha256HashCode = hash, RelSha256HashCode = relatedHash, Data = data }, hash);
                await session.SaveChangesAsync();
            }
        }

        public Task<bool> ValidateResource(IRelatedDataEntity entity) => ValidateResource(entity.Sha256HashCode, entity.RelSha256HashCode, entity.Data);

        public async Task<bool> ValidateResource(string hash, string relatedHash, Dictionary<string, HashSet<string>> data)
        {
            var aggregateData = await GetRelatedEntityData(hash);
            foreach (var kvp in data)
                if (!aggregateData.ContainsKey(kvp.Key))
                    aggregateData.Add(kvp.Key, kvp.Value);
                else
                    aggregateData[kvp.Key].UnionWith(kvp.Value);

            var rules = await GetValidationRules(aggregateData);

            foreach (var requirement in rules.Where(rule => rule.Conditions.All(condition => aggregateData.ContainsKey(condition.Key) && condition.Value.Equals(aggregateData[condition.Key]))).SelectMany(rule => rule.Requirements))
            {
                switch (requirement)
                {
                    case SingleValueRequirementRule req:
                        if (aggregateData.ContainsKey(req.PropertyName) && aggregateData[req.PropertyName].Count > 1)
                            return false;
                        break;
                    case ListValueRequirementRule req:
                        if (aggregateData.ContainsKey(req.PropertyName) && aggregateData[req.PropertyName].Any(t => !req.Values.Contains(t)))
                            return false;
                        break;
                }
            }
            return true;
        }

        public async Task<Dictionary<string, HashSet<string>>> GetRelatedEntityData(string hash)
        {
            using (var session = _store.OpenAsyncSession())
            {
                return await session.Advanced.AsyncDocumentQuery<KeyValuePair<string, HashSet<string>>, Index_RelatedDataAgg>().WhereEquals("Hashes", hash).ToListAsync().ContinueWith(t => t.Result.ToDictionary(s => s.Key, s => s.Value));
            }
        }

        public async Task<RelatedDataEntity> GetResource(string hash)
        {
            using (var session = _store.OpenAsyncSession())
            {
                return await session.LoadAsync<RelatedDataEntity>(hash);
            }
        }

        public async Task<string> AddValidationrule(RelatedDataValidationRule rule)
        {
            using (var session = _store.OpenAsyncSession())
            {
                await session.StoreAsync(rule);
                await session.SaveChangesAsync();
                return rule.Id;
            }
        }

        public async Task<RelatedDataValidationRule> GetValidationRule(string id)
        {
            using (var session = _store.OpenAsyncSession())
            {
                return await session.LoadAsync<RelatedDataValidationRule>(id);
            }
        }

        public async Task<List<RelatedDataValidationRule>> GetValidationRules(Dictionary<string, HashSet<string>> data)
        {
            using (var session = _store.OpenAsyncSession())
            {
                var propertyNames = data.Keys;

                // Find all the rules that could match
                var rules = session.Query<RelatedDataValidationRule>().AsQueryable();

                foreach (var key in data.Keys)
                    rules = rules.Where(t => t.Requirements.Any(s => s.PropertyName == key));

                return await rules.ToListAsync();
            }
        }
    }
}
