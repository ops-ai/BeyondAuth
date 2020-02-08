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
        public static Client FromModel(this ClientModel model)
        {
            return new Client
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
                DeviceCodeLifetime = model.DeviceCodeLifetime,
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
                RefreshTokenExpiration = model.RefreshTokenExpiration,
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
        /// <param name="model"></param>
        /// <returns></returns>
        public static ClientModel ToModel(this Client model)
        {
            return new ClientModel
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
                DeviceCodeLifetime = model.DeviceCodeLifetime,
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
                RefreshTokenExpiration = model.RefreshTokenExpiration,
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
    }
}
