using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeyondAuth.RelatedDataValidation
{
    public interface IRelatedDataAuthorizationService
    {
        Task AddResource(string hash, string relatedHash, Dictionary<string, List<string>> data);

        Task<bool> ValidateResource(string hash, string relatedHash, Dictionary<string, List<string>> data);

        Task AddResource(IRelatedDataEntity entity);

        Task<bool> ValidateResource(IRelatedDataEntity entity);

        Task<RelatedDataEntity> GetResource(string hash);

        Task AddValidationrule(RelatedDataValidationRule rule);

        Task<RelatedDataValidationRule> GetValidationRule(string id);

        Task<List<RelatedDataValidationRule>> GetValidationRules(Dictionary<string, List<string>> data);
    }
}
