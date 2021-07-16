using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Events;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using System;
using System.Threading.Tasks;

namespace Authentication.Infrastructure
{
    /// <summary>
    /// IdentityServer4 event sinks to RavenDb time series stats recorder
    /// </summary>
    public class IdentityServerStatsSink : IEventSink
    {
        private readonly ILogger _logger;
        private readonly IDocumentStore _store;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="store"></param>
        /// <param name="identityStoreOptions"></param>
        public IdentityServerStatsSink(ILogger<IdentityServerStatsSink> logger, IDocumentStore store, IOptions<IdentityStoreOptions> identityStoreOptions)
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
            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                switch (evt)
                {
                    case ApiAuthenticationSuccessEvent authenticationSuccessEvent:
                        session.TimeSeriesFor($"ApiResources/{authenticationSuccessEvent.ApiName}", evt.Name).Append(evt.TimeStamp, 1);
                        break;
                    case ApiAuthenticationFailureEvent apiAuthenticationFailureEvent:
                        session.TimeSeriesFor($"ApiResources/{apiAuthenticationFailureEvent.ApiName}", evt.Name).Append(evt.TimeStamp, 1);
                        break;

                    case ClientAuthenticationSuccessEvent clientAuthSuccess:
                        session.TimeSeriesFor($"Clients/{clientAuthSuccess.ClientId}", evt.Name).Append(evt.TimeStamp, 1);
                        break;
                    case ClientAuthenticationFailureEvent clientAuthFailure:
                        session.TimeSeriesFor($"Clients/{clientAuthFailure.ClientId}", evt.Name).Append(evt.TimeStamp, 1);
                        break;
                    case TokenIssuedSuccessEvent tokenIssuedSuccess:
                        session.TimeSeriesFor($"Clients/{tokenIssuedSuccess.ClientId}", evt.Name).Append(evt.TimeStamp, 1, tokenIssuedSuccess.SubjectId);
                        session.TimeSeriesFor(tokenIssuedSuccess.SubjectId, evt.Name).Append(evt.TimeStamp, 1, $"Clients/{tokenIssuedSuccess.ClientId}");
                        break;
                    case TokenIssuedFailureEvent tokenIssuedFailure:
                        session.TimeSeriesFor($"Clients/{tokenIssuedFailure.ClientId}", evt.Name).Append(evt.TimeStamp, 1, tokenIssuedFailure.SubjectId);
                        session.TimeSeriesFor(tokenIssuedFailure.SubjectId, evt.Name).Append(evt.TimeStamp, 1, $"Clients/{tokenIssuedFailure.ClientId}");
                        break;
                    case TokenIntrospectionSuccessEvent tokenIntrospectionSuccess:
                        session.TimeSeriesFor($"ApiResources/{tokenIntrospectionSuccess.ApiName}", evt.Name).Append(evt.TimeStamp, 1);
                        break;
                    case TokenIntrospectionFailureEvent tokenIntrospectionFailure:
                        session.TimeSeriesFor($"ApiResources/{tokenIntrospectionFailure.ApiName}", evt.Name).Append(evt.TimeStamp, 1);
                        break;
                    case TokenRevokedSuccessEvent tokenRevokedSuccess:
                        session.TimeSeriesFor($"Clients/{tokenRevokedSuccess.ClientId}", evt.Name).Append(evt.TimeStamp, 1);
                        break;
                    case UserLoginSuccessEvent userLoginSuccess:
                        session.TimeSeriesFor($"Clients/{userLoginSuccess.ClientId}", evt.Name).Append(evt.TimeStamp, 1, userLoginSuccess.SubjectId);
                        session.TimeSeriesFor(userLoginSuccess.SubjectId, evt.Name).Append(evt.TimeStamp, 1, $"Clients/{userLoginSuccess.ClientId}");
                        break;
                    case UserLoginFailureEvent userLoginFailure:
                        session.TimeSeriesFor($"Clients/{userLoginFailure.ClientId}", evt.Name).Append(evt.TimeStamp, 1);
                        break;
                    case UserLogoutSuccessEvent userLogoutSuccess:
                        session.TimeSeriesFor(userLogoutSuccess.SubjectId, evt.Name).Append(evt.TimeStamp, 1);
                        break;
                    case ConsentGrantedEvent consentGranted:
                        session.TimeSeriesFor($"Clients/{consentGranted.ClientId}", evt.Name).Append(evt.TimeStamp, 1, consentGranted.SubjectId);
                        session.TimeSeriesFor(consentGranted.SubjectId, evt.Name).Append(evt.TimeStamp, 1, $"Clients/{consentGranted.ClientId}");
                        break;
                    case ConsentDeniedEvent consentDenied:
                        session.TimeSeriesFor($"Clients/{consentDenied.ClientId}", evt.Name).Append(evt.TimeStamp, 1, consentDenied.SubjectId);
                        session.TimeSeriesFor(consentDenied.SubjectId, evt.Name).Append(evt.TimeStamp, 1, $"Clients/{consentDenied.ClientId}");
                        break;

                    case DeviceAuthorizationSuccessEvent deviceAuthorizationSuccess:
                        session.TimeSeriesFor($"Clients/{deviceAuthorizationSuccess.ClientId}", evt.Name).Append(evt.TimeStamp, 1);
                        break;
                    case DeviceAuthorizationFailureEvent deviceAuthorizationFailure:
                        session.TimeSeriesFor($"Clients/{deviceAuthorizationFailure.ClientId}", evt.Name).Append(evt.TimeStamp, 1);
                        break;
                }
                await session.SaveChangesAsync();
            }
        }
    }
}
