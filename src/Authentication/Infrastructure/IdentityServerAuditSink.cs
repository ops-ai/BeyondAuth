using Audit.Core;
using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Events;
using IdentityServer4.Services;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using System.Collections.Concurrent;
using System.Text;

namespace Authentication.Infrastructure
{
    /// <summary>
    /// IdentityServer4 event sinks to RavenDb time series stats recorder
    /// </summary>
    public class IdentityServerAuditSink : IEventSink
    {
        private readonly ILogger _logger;
        private readonly IDocumentStore _store;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;
        private readonly ConcurrentDictionary<string, int> _stats = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="store"></param>
        /// <param name="identityStoreOptions"></param>
        public IdentityServerAuditSink(ILogger<IdentityServerAuditSink> logger, IDocumentStore store, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _logger = logger ?? throw new ArgumentException("loggerFactory is required", nameof(logger));
            _store = store ?? throw new ArgumentException("store is required", nameof(store));
            _identityStoreOptions = identityStoreOptions;
        }

        /// <summary>
        /// Persist events
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        public async Task PersistAsync(Event evt)
        {
            switch (evt)
            {
                case ApiAuthenticationSuccessEvent authenticationSuccessEvent:
                    await AuditScope.LogAsync($"ApiResource:{evt.Name}", new { ResourceId = $"ApiResources/{authenticationSuccessEvent.ApiName}", authenticationSuccessEvent.RemoteIpAddress, authenticationSuccessEvent.AuthenticationMethod, authenticationSuccessEvent.Message });
                    break;
                case ApiAuthenticationFailureEvent apiAuthenticationFailureEvent:
                    await AuditScope.LogAsync($"ApiResource:{evt.Name}", new { ResourceId = $"ApiResources/{apiAuthenticationFailureEvent.ApiName}", apiAuthenticationFailureEvent.RemoteIpAddress, apiAuthenticationFailureEvent.Message });
                    break;
                case ClientAuthenticationSuccessEvent clientAuthSuccess:
                    await AuditScope.LogAsync($"Client:{evt.Name}", new { ClientId = $"Clients/{clientAuthSuccess.ClientId}", clientAuthSuccess.RemoteIpAddress, clientAuthSuccess.AuthenticationMethod, clientAuthSuccess.Message });
                    break;
                case ClientAuthenticationFailureEvent clientAuthFailure:
                    await AuditScope.LogAsync($"Client:{evt.Name}", new { ClientId = $"Clients/{clientAuthFailure.ClientId}", clientAuthFailure.RemoteIpAddress, clientAuthFailure.Message });
                    break;
                case TokenIssuedSuccessEvent tokenIssuedSuccess:

                    break;
                case TokenIssuedFailureEvent tokenIssuedFailure:
                    
                    break;
                case TokenIntrospectionSuccessEvent tokenIntrospectionSuccess:
                    await AuditScope.LogAsync($"ApiResource:{evt.Name}", new { ResourceId = $"ApiResources/{tokenIntrospectionSuccess.ApiName}", tokenIntrospectionSuccess.RemoteIpAddress, tokenIntrospectionSuccess.TokenScopes, tokenIntrospectionSuccess.Message });
                    break;
                case TokenIntrospectionFailureEvent tokenIntrospectionFailure:
                    await AuditScope.LogAsync($"ApiResource:{evt.Name}", new { ResourceId = $"ApiResources/{tokenIntrospectionFailure.ApiName}", tokenIntrospectionFailure.RemoteIpAddress, tokenIntrospectionFailure.TokenScopes, tokenIntrospectionFailure.Message });
                    break;
                case TokenRevokedSuccessEvent tokenRevokedSuccess:
                    await AuditScope.LogAsync($"Client:{evt.Name}", new { ClientId = $"Clients/{tokenRevokedSuccess.ClientId}", tokenRevokedSuccess.RemoteIpAddress, tokenRevokedSuccess.Message });
                    break;
                case UserLoginSuccessEvent userLoginSuccess:
                    await AuditScope.LogAsync($"User:{evt.Name}", new { userLoginSuccess.SubjectId, ClientId = $"Clients/{userLoginSuccess.ClientId}", userLoginSuccess.RemoteIpAddress, userLoginSuccess.Username, userLoginSuccess.Message });
                    break;
                case UserLoginFailureEvent userLoginFailure:
                    await AuditScope.LogAsync($"User:{evt.Name}", new { ClientId = $"Clients/{userLoginFailure.ClientId}", userLoginFailure.Username, userLoginFailure.RemoteIpAddress, userLoginFailure.Message });
                    break;
                case UserLogoutSuccessEvent userLogoutSuccess:
                    await AuditScope.LogAsync($"User:{evt.Name}", new { userLogoutSuccess.SubjectId, userLogoutSuccess.RemoteIpAddress, userLogoutSuccess.Message });
                    break;
                case ConsentGrantedEvent consentGranted:
                    await AuditScope.LogAsync($"User:{evt.Name}", new { consentGranted.SubjectId, ClientId = $"Clients/{consentGranted.ClientId}", consentGranted.RemoteIpAddress, consentGranted.Message, consentGranted.RequestedScopes });
                    break;
                case ConsentDeniedEvent consentDenied:
                    await AuditScope.LogAsync($"User:{evt.Name}", new { consentDenied.SubjectId, ClientId = $"Clients/{consentDenied.ClientId}", consentDenied.RemoteIpAddress, consentDenied.Message, consentDenied.RequestedScopes });
                    break;
                case DeviceAuthorizationSuccessEvent deviceAuthorizationSuccess:
                    await AuditScope.LogAsync($"Client:{evt.Name}", new { deviceAuthorizationSuccess.Scopes, ClientId = $"Clients/{deviceAuthorizationSuccess.ClientId}", deviceAuthorizationSuccess.RemoteIpAddress, deviceAuthorizationSuccess.Message });
                    break;
                case DeviceAuthorizationFailureEvent deviceAuthorizationFailure:
                    await AuditScope.LogAsync($"Client:{evt.Name}", new { deviceAuthorizationFailure.Scopes, ClientId = $"Clients/{deviceAuthorizationFailure.ClientId}", deviceAuthorizationFailure.RemoteIpAddress, deviceAuthorizationFailure.Message, deviceAuthorizationFailure.Error });
                    break;
            }
        }
    }
}
