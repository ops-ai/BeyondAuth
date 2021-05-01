using IdentityManager.Domain;
using IdentityManager.Models;

namespace IdentityManager.Extensions
{
    public static class ApiScopeModelExtensions
    {
        /// <summary>
        /// Create a ClientModel from a Client
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApiScopeEntity FromModel(this ApiScopeModel model)
        {
            return new ApiScopeEntity
            {
                Description = model.Description,
                DisplayName = model.DisplayName,
                Enabled = model.Enabled,
                Name = model.Name,
                Required = model.Required,
                Emphasize = model.Emphasize,
                ShowInDiscoveryDocument = model.ShowInDiscoveryDocument,
                Properties = model.Properties,
                UserClaims = model.UserClaims
            };
        }

        /// <summary>
        /// Convert a Client to a ClientModel
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ApiScopeModel ToModel(this ApiScopeEntity entity)
        {
            return new ApiScopeModel
            {
                Description = entity.Description,
                DisplayName = entity.DisplayName,
                Enabled = entity.Enabled,
                Name = entity.Name,
                Required = entity.Required,
                Emphasize = entity.Emphasize,
                ShowInDiscoveryDocument = entity.ShowInDiscoveryDocument,
                Properties = entity.Properties,
                UserClaims = entity.UserClaims
            };
        }
    }
}
