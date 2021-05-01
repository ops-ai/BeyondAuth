using IdentityManager.Models;
using IdentityServer4.Models;
using System;

namespace IdentityManager.Extensions
{
    /// <summary>
    /// Helpers to convert
    /// </summary>
    public static class SecretModelExtensions
    {
        /// <summary>
        /// Create a SecretModel from a Secret
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static Secret FromModel(this SecretModel model)
        {
            return new Secret
            {
                Description = model.Description,
                Expiration = model.Expiration,
                Type = model.Type.ToString()
            };
        }

        /// <summary>
        /// Convert a Secret to a SecretModel
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static SecretModel ToModel(this Secret entity)
        {
            return new SecretModel
            {
                Id = entity.Value.Sha256(),
                Description = entity.Description,
                Expiration = entity.Expiration,
                Type = Enum.Parse<SecretTypes>(entity.Type)
            };
        }
    }
}
