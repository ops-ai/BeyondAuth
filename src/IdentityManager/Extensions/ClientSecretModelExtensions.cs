using IdentityManager.Domain;
using IdentityManager.Models;
using System.Linq;

namespace IdentityManager.Extensions
{
    public static class ClientSecretModelExtensions
    {
        /// <summary>
        /// Create a ClientModel from a Client Secret
        /// </summary>
        /// <param name="model"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public static ClientSecretEntity FromModel(this ClientSecretModel model, string clientId)
        {
            return new ClientSecretEntity
            {
                Id = $"ClientSecrets/{clientId}/{model.Id}",
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
                Id = entity.Id.Split('/').Last(),
                Description = entity.Description,
                Expiration = entity.Expiration,
                Type = entity.Type
            };
        }
    }
}
