using IdentityManager.Domain;
using IdentityManager.Models;
using IdentityServer4.Models;

namespace IdentityManager.Extensions
{
    public static class ClientModelExtensions
    {
        /// <summary>
        /// Create a ClientModel from a Client
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ClientEntity FromModel(this ClientModel model)
        {
            return new ClientEntity
            {
                AbsoluteRefreshTokenLifetime = model.AbsoluteRefreshTokenLifetime,
                AccessTokenLifetime = model.AccessTokenLifetime,
                AccessTokenType = model.AccessTokenType,
                AllowAccessTokensViaBrowser = model.AllowAccessTokensViaBrowser,
                AllowedCorsOrigins = model.AllowedCorsOrigins,
                AllowedGrantTypes = model.AllowedGrantTypes,
                AllowedScopes = model.AllowedScopes,
                AllowOfflineAccess = model.AllowOfflineAccess,
                AllowPlainTextPkce = model.AllowPlainTextPkce,
                AllowRememberConsent = model.AllowRememberConsent,
                AlwaysIncludeUserClaimsInIdToken = model.AlwaysIncludeUserClaimsInIdToken,
                AlwaysSendClientClaims = model.AlwaysSendClientClaims,
                AuthorizationCodeLifetime = model.AuthorizationCodeLifetime,
                BackChannelLogoutSessionRequired = model.BackChannelLogoutSessionRequired,
                BackChannelLogoutUri = model.BackChannelLogoutUri,
                Claims = model.Claims,
                ClientClaimsPrefix = model.ClientClaimsPrefix,
                ClientId = model.ClientId,
                ClientName = model.ClientName,
                ClientUri = model.ClientUri,
                ConsentLifetime = model.ConsentLifetime,
                Description = model.Description,
                DeviceCodeLifetime = model.DeviceCodeLifetime ?? 0,
                Enabled = model.Enabled,
                EnableLocalLogin = model.EnableLocalLogin,
                FrontChannelLogoutSessionRequired = model.FrontChannelLogoutSessionRequired,
                FrontChannelLogoutUri = model.FrontChannelLogoutUri,
                IdentityProviderRestrictions = model.IdentityProviderRestrictions,
                IdentityTokenLifetime = model.IdentityTokenLifetime,
                IncludeJwtId = model.IncludeJwtId,
                LogoUri = model.LogoUri,
                PairWiseSubjectSalt = model.PairWiseSubjectSalt,
                PostLogoutRedirectUris = model.PostLogoutRedirectUris,
                Properties = model.Properties,
                ProtocolType = model.ProtocolType,
                RedirectUris = model.RedirectUris,
                RefreshTokenExpiration = model.RefreshTokenExpiration ?? TokenExpiration.Absolute,
                RefreshTokenUsage = model.RefreshTokenUsage,
                RequireClientSecret = model.RequireClientSecret,
                RequireConsent = model.RequireConsent,
                RequirePkce = model.RequirePkce,
                SlidingRefreshTokenLifetime = model.SlidingRefreshTokenLifetime,
                UpdateAccessTokenClaimsOnRefresh = model.UpdateAccessTokenClaimsOnRefresh,
                UserCodeType = model.UserCodeType,
                UserSsoLifetime = model.UserSsoLifetime
            };
        }

        /// <summary>
        /// Convert a Client to a ClientModel
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ClientModel ToModel(this ClientEntity entity)
        {
            return new ClientModel
            {
                AbsoluteRefreshTokenLifetime = entity.AbsoluteRefreshTokenLifetime,
                AccessTokenLifetime = entity.AccessTokenLifetime,
                AccessTokenType = entity.AccessTokenType,
                AllowAccessTokensViaBrowser = entity.AllowAccessTokensViaBrowser,
                AllowedCorsOrigins = entity.AllowedCorsOrigins,
                AllowedGrantTypes = entity.AllowedGrantTypes,
                AllowedScopes = entity.AllowedScopes,
                AllowOfflineAccess = entity.AllowOfflineAccess,
                AllowPlainTextPkce = entity.AllowPlainTextPkce,
                AllowRememberConsent = entity.AllowRememberConsent,
                AlwaysIncludeUserClaimsInIdToken = entity.AlwaysIncludeUserClaimsInIdToken,
                AlwaysSendClientClaims = entity.AlwaysSendClientClaims,
                AuthorizationCodeLifetime = entity.AuthorizationCodeLifetime,
                BackChannelLogoutSessionRequired = entity.BackChannelLogoutSessionRequired,
                BackChannelLogoutUri = entity.BackChannelLogoutUri,
                Claims = entity.Claims,
                ClientClaimsPrefix = entity.ClientClaimsPrefix,
                ClientId = entity.ClientId,
                ClientName = entity.ClientName,
                ClientUri = entity.ClientUri,
                ConsentLifetime = entity.ConsentLifetime,
                Description = entity.Description,
                DeviceCodeLifetime = entity.DeviceCodeLifetime,
                Enabled = entity.Enabled,
                EnableLocalLogin = entity.EnableLocalLogin,
                FrontChannelLogoutSessionRequired = entity.FrontChannelLogoutSessionRequired,
                FrontChannelLogoutUri = entity.FrontChannelLogoutUri,
                IdentityProviderRestrictions = entity.IdentityProviderRestrictions,
                IdentityTokenLifetime = entity.IdentityTokenLifetime,
                IncludeJwtId = entity.IncludeJwtId,
                LogoUri = entity.LogoUri,
                PairWiseSubjectSalt = entity.PairWiseSubjectSalt,
                PostLogoutRedirectUris = entity.PostLogoutRedirectUris,
                Properties = entity.Properties,
                ProtocolType = entity.ProtocolType,
                RedirectUris = entity.RedirectUris,
                RefreshTokenExpiration = entity.RefreshTokenExpiration,
                RefreshTokenUsage = entity.RefreshTokenUsage,
                RequireClientSecret = entity.RequireClientSecret,
                RequireConsent = entity.RequireConsent,
                RequirePkce = entity.RequirePkce,
                SlidingRefreshTokenLifetime = entity.SlidingRefreshTokenLifetime,
                UpdateAccessTokenClaimsOnRefresh = entity.UpdateAccessTokenClaimsOnRefresh,
                UserCodeType = entity.UserCodeType,
                UserSsoLifetime = entity.UserSsoLifetime
            };
        }
    }
}
