using IdentityManager.Domain;
using IdentityManager.Models;

namespace IdentityManager.Extensions
{
    public static class ClientSecretModelExtensions
    {
        /// <summary>
        /// Create a ClientModel from a Client
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ClientSecretEntity FromModel(this ClientSecretModel model)
        {
            return new ClientSecretEntity
            {
                Description = model.Description,
                Expiration = model.Expiration,
                Type = model.Type,
                Value = "generate"
            };
        }

        /// <summary>
        /// Convert a Client to a ClientModel
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ClientSecretModel ToModel(this ClientSecretEntity entity)
        {
            return new ClientSecretModel
            {
                Id = entity.Id,
                Description = entity.Description,
                Expiration = entity.Expiration,
                Type = entity.Type
            };
        }
    }
}
