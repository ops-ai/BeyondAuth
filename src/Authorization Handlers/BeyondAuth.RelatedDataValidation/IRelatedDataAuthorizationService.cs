using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeyondAuth.RelatedDataValidation
{
    public interface IRelatedDataAuthorizationService
    {
        Task AddResource(string hash, string relatedHash, Dictionary<string, HashSet<string>> data);

        Task<bool> ValidateResource(string hash, string relatedHash, Dictionary<string, HashSet<string>> data);

        Task AddResource(IRelatedDataEntity entity);

        Task<bool> ValidateResource(IRelatedDataEntity entity);

        Task<RelatedDataEntity> GetResource(string hash);

        Task<string> AddValidationrule(RelatedDataValidationRule rule);

        Task<RelatedDataValidationRule> GetValidationRule(string id);

        Task<List<RelatedDataValidationRule>> GetValidationRules(Dictionary<string, HashSet<string>> data);

        Task<Dictionary<string, HashSet<string>>> GetRelatedEntityData(string hash);
    }
}
