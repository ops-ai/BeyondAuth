using IdentityServer4.Events;
using System.Diagnostics.Tracing;

namespace Authentication.Infrastructure
{
    /// <summary>
    /// Event source implementation for IdentityServer4 event sinks
    /// </summary>
    [EventSource(Name = "IdentityServer")]
    public class IdentityServerEventSource : EventSource
    {
        /// <summary>
        /// 
        /// </summary>
        public static IdentityServerEventSource Log = new();

        /// <summary>
        /// Event keywords
        /// </summary>
        private class Keywords
        {
            public const EventKeywords Api = (EventKeywords)1;
            public const EventKeywords Client = (EventKeywords)2;
            public const EventKeywords Token = (EventKeywords)4;
            public const EventKeywords Perf = (EventKeywords)8;
            public const EventKeywords TokenIntrospection = (EventKeywords)16;
            public const EventKeywords TokenRevoked = (EventKeywords)32;
            public const EventKeywords UserLogin = (EventKeywords)64;
            public const EventKeywords UserLogout = (EventKeywords)128;
            public const EventKeywords Consent = (EventKeywords)256;
            public const EventKeywords UnhandledException = (EventKeywords)512;
            public const EventKeywords DeviceAuthorization = (EventKeywords)1024;
        }

        /// <summary>
        /// Api Authentication Success
        /// </summary>
        /// <param name="evt"></param>
        [Event(1, Message = "Api Authentication Success: {0}", Level = EventLevel.Informational, Keywords = Keywords.Api)]
        public void ApiAuthenticationSuccess(ApiAuthenticationSuccessEvent evt) { WriteEvent(1, evt.ApiName, evt); }

        /// <summary>
        /// Api Authentication Failure
        /// </summary>
        /// <param name="evt"></param>
        [Event(2, Message = "Api Authentication Failure: {0}", Level = EventLevel.Warning, Keywords = Keywords.Api)]
        public void ApiAuthenticationFailure(ApiAuthenticationFailureEvent evt) { WriteEvent(2, evt.ApiName, evt); }

        /// <summary>
        /// Client Authentication Success
        /// </summary>
        /// <param name="evt"></param>
        [Event(3, Message = "Client Authentication Success: {0}", Level = EventLevel.Informational, Keywords = Keywords.Client)]
        public void ClientAuthenticationSuccess(ClientAuthenticationSuccessEvent evt) { WriteEvent(3, evt.ClientId, evt); }

        /// <summary>
        /// Client Authentication Failure
        /// </summary>
        /// <param name="evt"></param>
        [Event(4, Message = "Client Authentication Failure: {0}", Level = EventLevel.Warning, Keywords = Keywords.Client)]
        public void ClientAuthenticationFailure(ClientAuthenticationFailureEvent evt) { WriteEvent(4, evt.ClientId, evt); }

        /// <summary>
        /// Token Issued Success
        /// </summary>
        /// <param name="evt"></param>
        [Event(5, Message = "Token Issued Success: {0}", Level = EventLevel.Informational, Keywords = Keywords.Token)]
        public void TokenIssuedSuccess(TokenIssuedSuccessEvent evt) { WriteEvent(5, evt.GrantType, evt); }

        /// <summary>
        /// Token Issued Failure
        /// </summary>
        /// <param name="evt"></param>
        [Event(6, Message = "Token Issued Failure: {0}", Level = EventLevel.Warning, Keywords = Keywords.Token)]
        public void TokenIssuedFailure(TokenIssuedFailureEvent evt) { WriteEvent(6, evt.GrantType, evt); }

        /// <summary>
        /// Token Introspection Success
        /// </summary>
        /// <param name="evt"></param>
        [Event(7, Message = "Token Introspection Success: {0}", Level = EventLevel.Informational, Keywords = Keywords.TokenIntrospection)]
        public void TokenIntrospectionSuccess(TokenIntrospectionSuccessEvent evt) { WriteEvent(7, evt.Token, evt); }

        /// <summary>
        /// Token Introspection Failure
        /// </summary>
        /// <param name="evt"></param>
        [Event(8, Message = "Token Introspection Failure: {0}", Level = EventLevel.Warning, Keywords = Keywords.TokenIntrospection)]
        public void TokenIntrospectionFailure(TokenIntrospectionFailureEvent evt) { WriteEvent(8, evt.Token, evt); }

        /// <summary>
        /// Token Revoked Success
        /// </summary>
        /// <param name="evt"></param>
        [Event(9, Message = "Token Revoked Success: {0}", Level = EventLevel.Informational, Keywords = Keywords.TokenRevoked)]
        public void TokenRevokedSuccess(TokenRevokedSuccessEvent evt) { WriteEvent(9, evt.ClientId, evt); }

        /// <summary>
        /// User Login Success
        /// </summary>
        /// <param name="evt"></param>
        [Event(10, Message = "User Login Success: {0}", Level = EventLevel.Informational, Keywords = Keywords.UserLogin)]
        public void UserLoginSuccess(UserLoginSuccessEvent evt) { WriteEvent(10, evt.ClientId, evt); }

        /// <summary>
        /// User Login Failure
        /// </summary>
        /// <param name="evt"></param>
        [Event(11, Message = "User Login Failure: {0}", Level = EventLevel.Warning, Keywords = Keywords.UserLogin)]
        public void UserLoginFailure(UserLoginFailureEvent evt) { WriteEvent(11, evt.ClientId, evt); }

        /// <summary>
        /// User Logout Success
        /// </summary>
        /// <param name="evt"></param>
        [Event(12, Message = "User Logout Success: {0}", Level = EventLevel.Informational, Keywords = Keywords.UserLogout)]
        public void UserLogoutSuccess(UserLogoutSuccessEvent evt) { WriteEvent(12, evt.SubjectId, evt); }

        /// <summary>
        /// Consent Granted
        /// </summary>
        /// <param name="evt"></param>
        [Event(13, Message = "Consent Granted: {0}", Level = EventLevel.Informational, Keywords = Keywords.Consent)]
        public void ConsentGranted(ConsentGrantedEvent evt) { WriteEvent(13, evt.ClientId, evt); }

        /// <summary>
        /// Consent Denied
        /// </summary>
        /// <param name="evt"></param>
        [Event(14, Message = "Consent Denied: {0}", Level = EventLevel.Informational, Keywords = Keywords.Consent)]
        public void ConsentDenied(ConsentDeniedEvent evt) { WriteEvent(14, evt.ClientId, evt); }

        /// <summary>
        /// Unhandled Exception
        /// </summary>
        /// <param name="evt"></param>
        [Event(15, Message = "Unhandled Exception: {0}", Level = EventLevel.Error, Keywords = Keywords.UnhandledException)]
        public void UnhandledException(UnhandledExceptionEvent evt) { WriteEvent(15, evt.Category, evt); }

        /// <summary>
        /// Device Authorization Success
        /// </summary>
        /// <param name="evt"></param>
        [Event(16, Message = "Device Authorization Success: {0}", Level = EventLevel.Informational, Keywords = Keywords.DeviceAuthorization)]
        public void DeviceAuthorizationSuccess(DeviceAuthorizationSuccessEvent evt) { WriteEvent(16, evt.ClientId, evt); }

        /// <summary>
        /// Device Authorization Failure
        /// </summary>
        /// <param name="evt"></param>
        [Event(17, Message = "Device Authorization Failure: {0}", Level = EventLevel.Warning, Keywords = Keywords.DeviceAuthorization)]
        public void DeviceAuthorizationFailure(DeviceAuthorizationFailureEvent evt) { WriteEvent(17, evt.ClientId, evt); }
    }
}
