using IdentityManager.Domain;
using IdentityManager.Models;

namespace IdentityManager.Extensions
{
    public static class IdentityResourceModelExtensions
    {
        /// <summary>
        /// Create a ClientModel from a Client
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static IdentityResourceEntity FromModel(this IdentityResourceModel model)
        {
            return new IdentityResourceEntity
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
        public static IdentityResourceModel ToModel(this IdentityResourceEntity entity)
        {
            return new IdentityResourceModel
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
