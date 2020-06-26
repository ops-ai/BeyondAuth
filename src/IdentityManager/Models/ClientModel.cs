using IdentityServer4;
using IdentityServer4.Models;
using System.Collections.Generic;
using System.Security.Claims;

namespace IdentityManager.Models
{
    /// <summary>
    /// Client configuration
    /// </summary>
    public class ClientModel
    {
        /// <summary>
        /// Unique ID of the client
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Specifies if client is enabled (defaults to <c>true</c>)
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [allow offline access]. Defaults to false.
        /// </summary>
        public bool AllowOfflineAccess { get; set; }

        /// <summary>
        /// Lifetime of identity token in seconds (defaults to 900 seconds / 15 minutes)
        /// </summary>
        public int IdentityTokenLifetime { get; set; } = 900;

        /// <summary>
        /// Lifetime of access token in seconds (defaults to 900 seconds / 15 minutes)
        /// </summary>
        public int AccessTokenLifetime { get; set; } = 900;

        /// <summary>
        /// Lifetime of authorization code in seconds (defaults to 30 seconds)
        /// </summary>
        public int AuthorizationCodeLifetime { get; set; } = 30;

        /// <summary>
        /// Maximum lifetime of a refresh token in seconds. Defaults to 2592000 seconds /
        /// 30 days
        /// </summary>
        public int AbsoluteRefreshTokenLifetime { get; set; } = 2592000;

        /// <summary>
        /// Sliding lifetime of a refresh token in seconds. Defaults to 1296000 seconds /
        /// 15 days
        /// </summary>
        public int SlidingRefreshTokenLifetime { get; set; } = 1296000;

        /// <summary>
        /// Lifetime of a user consent in seconds. Defaults to null (no expiration)
        /// </summary>
        public int? ConsentLifetime { get; set; }

        /// <summary>
        /// ReUse: the refresh token handle will stay the same when refreshing tokens 
        /// OneTime: the refresh token handle will be updated when refreshing tokens
        /// </summary>
        public TokenUsage RefreshTokenUsage { get; set; } = TokenUsage.ReUse;

        /// <summary>
        /// Gets or sets a value indicating whether the access token (and its claims) should
        /// be updated on a refresh token request. Defaults to false.
        /// </summary>
        public bool UpdateAccessTokenClaimsOnRefresh { get; set; } = false;

        /// <summary>
        /// Absolute: the refresh token will expire on a fixed point in time (specified by
        /// the AbsoluteRefreshTokenLifetime)
        /// Sliding: when refreshing the token, the lifetime of the refresh token will be 
        /// renewed (by the amount specified in SlidingRefreshTokenLifetime).
        /// The lifetime will not exceed AbsoluteRefreshTokenLifetime.
        /// </summary>
        public TokenExpiration RefreshTokenExpiration { get; set; }

        /// <summary>
        /// Specifies whether the access token is a reference token or a self contained JWT
        /// token (defaults to Jwt).
        /// </summary>
        public AccessTokenType AccessTokenType { get; set; } = AccessTokenType.Jwt;

        /// <summary>
        /// Gets or sets a value indicating whether the local login is allowed for this client.
        /// Defaults to true.
        /// </summary>
        public bool EnableLocalLogin { get; set; } = true;

        /// <summary>
        /// Specifies which external IdPs can be used with this client (if list is empty
        /// all IdPs are allowed). Defaults to empty.
        /// </summary>
        public ICollection<string> IdentityProviderRestrictions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether JWT access tokens should include an identifier.
        /// Defaults to false.
        /// </summary>
        public bool IncludeJwtId { get; set; } = false;

        /// <summary>
        /// Allows settings claims for the client (will be included in the access token).
        /// </summary>
        public ICollection<ClientClaim> Claims { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether client claims should be always included
        /// in the access tokens - or only for client credentials flow. Defaults to false
        /// </summary>
        public bool AlwaysSendClientClaims { get; set; } = false;

        /// <summary>
        /// Gets or sets a value to prefix it on client claim types. Defaults to client_.
        /// </summary>
        public string ClientClaimsPrefix { get; set; }

        /// <summary>
        /// Gets or sets a salt value used in pair-wise subjectId generation for users of
        /// this client.
        /// </summary>
        public string PairWiseSubjectSalt { get; set; }

        /// <summary>
        /// The maximum duration (in seconds) since the last time the user authenticated.
        /// </summary>
        public int? UserSsoLifetime { get; set; }

        /// <summary>
        /// Gets or sets the type of the device flow user code.
        /// </summary>
        public string UserCodeType { get; set; }

        /// <summary>
        /// Gets or sets the device code lifetime.
        /// </summary>
        public int DeviceCodeLifetime { get; set; }

        /// <summary>
        /// When requesting both an id token and access token, should the user claims always
        /// be added to the id token instead of requring the client to use the userinfo endpoint.
        /// Defaults to false.
        /// </summary>
        public bool AlwaysIncludeUserClaimsInIdToken { get; set; } = false;

        /// <summary>
        /// Specifies the api scopes that the client is allowed to request. If empty, the
        /// client can't access any scope
        /// </summary>
        public ICollection<string> AllowedScopes { get; set; } = new List<string>();

        /// <summary>
        /// Specifies is the user's session id should be sent to the BackChannelLogoutUri.
        /// Defaults to true.
        /// </summary>
        public bool BackChannelLogoutSessionRequired { get; set; } = true;

        /// <summary>
        /// Gets or sets the protocol type.
        /// </summary>
        /// <value>
        /// The protocol type.
        /// </value>
        public string ProtocolType { get; set; } = IdentityServerConstants.ProtocolTypes.OpenIdConnect;

        /// <summary>
        /// If set to false, no client secret is needed to request tokens at the token endpoint (defaults to <c>true</c>)
        /// </summary>
        public bool RequireClientSecret { get; set; } = true;

        /// <summary>
        /// Client display name (used for logging and consent screen)
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// Description of the client.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// URI to further information about client (used on consent screen)
        /// </summary>
        public string ClientUri { get; set; }

        /// <summary>
        /// URI to client logo (used on consent screen)
        /// </summary>
        public string LogoUri { get; set; }

        /// <summary>
        /// Gets or sets the allowed CORS origins for JavaScript clients.
        /// </summary>
        public ICollection<string> AllowedCorsOrigins { get; set; }

        //// <summary>
        /// Specifies whether a consent screen is required (defaults to <c>false</c>)
        /// </summary>
        public bool RequireConsent { get; set; }

        /// <summary>
        /// Specifies the allowed grant types (legal combinations of AuthorizationCode, Implicit,
        /// Hybrid, ResourceOwner, ClientCredentials).
        /// </summary>
        public ICollection<string> AllowedGrantTypes { get; set; }

        /// <summary>
        /// Specifies whether a proof key is required for authorization code based token
        /// requests (defaults to false).
        /// </summary>
        public bool RequirePkce { get; set; }

        /// <summary>
        /// Specifies whether a proof key can be sent using plain method (not recommended
        /// and defaults to false.)
        /// </summary>
        public bool AllowPlainTextPkce { get; set; } = false;

        /// <summary>
        /// Controls whether access tokens are transmitted via the browser for this client
        /// (defaults to false). This can prevent accidental leakage of access tokens when
        /// multiple response types are allowed.
        /// </summary>
        public bool AllowAccessTokensViaBrowser { get; set; } = false;

        /// <summary>
        /// Specifies allowed URIs to return tokens or authorization codes to
        /// </summary>
        public ICollection<string> RedirectUris { get; set; } = new List<string>();

        /// <summary>
        /// Specifies allowed URIs to redirect to after logout
        /// </summary>
        public ICollection<string> PostLogoutRedirectUris { get; set; } = new List<string>();

        /// <summary>
        /// Specifies logout URI at client for HTTP front-channel based logout.
        /// </summary>
        public string FrontChannelLogoutUri { get; set; }

        /// <summary>
        /// Specifies is the user's session id should be sent to the FrontChannelLogoutUri.
        /// Defaults to true.
        /// </summary>
        public bool FrontChannelLogoutSessionRequired { get; set; } = true;

        /// <summary>
        /// Specifies logout URI at client for HTTP back-channel based logout.
        /// </summary>
        public string BackChannelLogoutUri { get; set; }

        /// <summary>
        /// Specifies whether user can choose to store consent decisions (defaults to true)
        /// </summary>
        public bool AllowRememberConsent { get; set; } = true;

        /// <summary>
        /// Gets or sets the custom properties for the client.
        /// </summary>
        public IDictionary<string, string> Properties { get; set; }
    }
}
