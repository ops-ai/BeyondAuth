using IdentityServer4.Events;
using IdentityServer4.Services;
using System.Diagnostics;

namespace Authentication.Infrastructure
{
    /// <summary>
    /// IdentityServer4 event sinks to EventSource handler
    /// </summary>
    public class IdentityServerEventSink : IEventSink
    {
        private static readonly ActivitySource ActivitySource = new ActivitySource(nameof(IdentityServerEventSink));

        /// <summary>
        /// Persist events
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        public Task PersistAsync(Event evt)
        {
            using (var activity = ActivitySource.StartActivity(evt.Name, ActivityKind.Server))
            {
                activity.SetTag(nameof(evt.ActivityId), evt.ActivityId);
                activity.SetTag(nameof(evt.EventType), evt.EventType);
                activity.SetTag(nameof(evt.RemoteIpAddress), evt.RemoteIpAddress);
                activity.SetTag(nameof(evt.Category), evt.Category);
                activity.SetStartTime(evt.TimeStamp);

                switch (evt)
                {
                    case ApiAuthenticationSuccessEvent authenticationSuccessEvent:
                        activity.AddTag(nameof(authenticationSuccessEvent.ApiName), authenticationSuccessEvent.ApiName);
                        activity.AddTag(nameof(authenticationSuccessEvent.AuthenticationMethod), authenticationSuccessEvent.AuthenticationMethod);
                        IdentityServerEventSource.Log.ApiAuthenticationSuccess(authenticationSuccessEvent);
                        break;
                    case ApiAuthenticationFailureEvent apiAuthenticationFailureEvent:
                        activity.AddTag(nameof(apiAuthenticationFailureEvent.ApiName), apiAuthenticationFailureEvent.ApiName);
                        IdentityServerEventSource.Log.ApiAuthenticationFailure(apiAuthenticationFailureEvent);
                        break;

                    case ClientAuthenticationSuccessEvent clientAuthSuccess:
                        activity.AddTag(nameof(clientAuthSuccess.ClientId), clientAuthSuccess.ClientId);
                        activity.AddTag(nameof(clientAuthSuccess.AuthenticationMethod), clientAuthSuccess.AuthenticationMethod);
                        IdentityServerEventSource.Log.ClientAuthenticationSuccess(clientAuthSuccess);
                        break;
                    case ClientAuthenticationFailureEvent clientAuthFailure:
                        activity.AddTag(nameof(clientAuthFailure.ClientId), clientAuthFailure.ClientId);
                        IdentityServerEventSource.Log.ClientAuthenticationFailure(clientAuthFailure);
                        break;
                    case TokenIssuedSuccessEvent tokenIssuedSuccess:
                        activity.AddTag(nameof(tokenIssuedSuccess.ClientId), tokenIssuedSuccess.ClientId);
                        activity.AddTag(nameof(tokenIssuedSuccess.ClientName), tokenIssuedSuccess.ClientName);
                        activity.AddTag(nameof(tokenIssuedSuccess.GrantType), tokenIssuedSuccess.GrantType);
                        activity.AddTag(nameof(tokenIssuedSuccess.SubjectId), tokenIssuedSuccess.SubjectId);
                        IdentityServerEventSource.Log.TokenIssuedSuccess(tokenIssuedSuccess);
                        break;
                    case TokenIssuedFailureEvent tokenIssuedFailure:
                        activity.AddTag(nameof(tokenIssuedFailure.ClientId), tokenIssuedFailure.ClientId);
                        activity.AddTag(nameof(tokenIssuedFailure.ClientName), tokenIssuedFailure.ClientName);
                        activity.AddTag(nameof(tokenIssuedFailure.GrantType), tokenIssuedFailure.GrantType);
                        activity.AddTag(nameof(tokenIssuedFailure.SubjectId), tokenIssuedFailure.SubjectId);
                        IdentityServerEventSource.Log.TokenIssuedFailure(tokenIssuedFailure);
                        break;
                    case TokenIntrospectionSuccessEvent tokenIntrospectionSuccess:
                        activity.AddTag(nameof(tokenIntrospectionSuccess.ApiName), tokenIntrospectionSuccess.ApiName);
                        IdentityServerEventSource.Log.TokenIntrospectionSuccess(tokenIntrospectionSuccess);
                        break;
                    case TokenIntrospectionFailureEvent tokenIntrospectionFailure:
                        activity.AddTag(nameof(tokenIntrospectionFailure.ApiName), tokenIntrospectionFailure.ApiName);
                        IdentityServerEventSource.Log.TokenIntrospectionFailure(tokenIntrospectionFailure);
                        break;
                    case TokenRevokedSuccessEvent tokenRevokedSuccess:
                        activity.AddTag(nameof(tokenRevokedSuccess.ClientId), tokenRevokedSuccess.ClientId);
                        activity.AddTag(nameof(tokenRevokedSuccess.ClientName), tokenRevokedSuccess.ClientName);
                        activity.AddTag(nameof(tokenRevokedSuccess.TokenType), tokenRevokedSuccess.TokenType);
                        IdentityServerEventSource.Log.TokenRevokedSuccess(tokenRevokedSuccess);
                        break;
                    case UserLoginSuccessEvent userLoginSuccess:
                        activity.AddTag(nameof(userLoginSuccess.ClientId), userLoginSuccess.ClientId);
                        activity.AddTag(nameof(userLoginSuccess.Provider), userLoginSuccess.Provider);
                        activity.AddTag(nameof(userLoginSuccess.ProviderUserId), userLoginSuccess.ProviderUserId);
                        activity.AddTag(nameof(userLoginSuccess.SubjectId), userLoginSuccess.SubjectId);
                        activity.AddTag(nameof(userLoginSuccess.Username), userLoginSuccess.Username);
                        IdentityServerEventSource.Log.UserLoginSuccess(userLoginSuccess);
                        break;
                    case UserLoginFailureEvent userLoginFailure:
                        activity.AddTag(nameof(userLoginFailure.ClientId), userLoginFailure.ClientId);
                        activity.AddTag(nameof(userLoginFailure.Username), userLoginFailure.Username);
                        IdentityServerEventSource.Log.UserLoginFailure(userLoginFailure);
                        break;
                    case UserLogoutSuccessEvent userLogoutSuccess:
                        activity.AddTag(nameof(userLogoutSuccess.SubjectId), userLogoutSuccess.SubjectId);
                        IdentityServerEventSource.Log.UserLogoutSuccess(userLogoutSuccess);
                        break;
                    case ConsentGrantedEvent consentGranted:
                        activity.AddTag(nameof(consentGranted.ClientId), consentGranted.ClientId);
                        activity.AddTag(nameof(consentGranted.SubjectId), consentGranted.SubjectId);
                        IdentityServerEventSource.Log.ConsentGranted(consentGranted);
                        break;
                    case ConsentDeniedEvent consentDenied:
                        activity.AddTag(nameof(consentDenied.ClientId), consentDenied.ClientId);
                        activity.AddTag(nameof(consentDenied.SubjectId), consentDenied.SubjectId);
                        IdentityServerEventSource.Log.ConsentDenied(consentDenied);
                        break;
                    case UnhandledExceptionEvent unhandledException:
                        IdentityServerEventSource.Log.UnhandledException(unhandledException);
                        break;
                    case DeviceAuthorizationSuccessEvent deviceAuthorizationSuccess:
                        activity.AddTag(nameof(deviceAuthorizationSuccess.ClientId), deviceAuthorizationSuccess.ClientId);
                        activity.AddTag(nameof(deviceAuthorizationSuccess.ClientName), deviceAuthorizationSuccess.ClientName);
                        IdentityServerEventSource.Log.DeviceAuthorizationSuccess(deviceAuthorizationSuccess);
                        break;
                    case DeviceAuthorizationFailureEvent deviceAuthorizationFailure:
                        activity.AddTag(nameof(deviceAuthorizationFailure.ClientId), deviceAuthorizationFailure.ClientId);
                        activity.AddTag(nameof(deviceAuthorizationFailure.ClientName), deviceAuthorizationFailure.ClientName);
                        IdentityServerEventSource.Log.DeviceAuthorizationFailure(deviceAuthorizationFailure);
                        break;
                }
            }

            return Task.CompletedTask;
        }
    }
}
