using Authentication.Domain;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Raven.Client.Documents.Session;

namespace Authentication.Services
{
    public class RavenDbSessionProvider : DefaultUserSession
    {
        private readonly IAsyncDocumentSession _session;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultUserSession"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="handlers">The handlers.</param>
        /// <param name="options">The options.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="session">Raven document session</param>
        public RavenDbSessionProvider(
            IHttpContextAccessor httpContextAccessor,
            IAuthenticationHandlerProvider handlers,
            IdentityServerOptions options,
            ISystemClock clock,
            ILogger<IUserSession> logger, IAsyncDocumentSession session) : base(httpContextAccessor, handlers, options, clock, logger)
        {
            _session = session;
        }

        public override async Task AddClientIdAsync(string clientId)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));

            await base.AddClientIdAsync(clientId);

            await AuthenticateAsync();
            var sessionId = Properties?.GetSessionId();

            var userSession = await _session.LoadAsync<UserSession>($"UserSessions/{sessionId}");
            if (userSession != null)
            {
                if (userSession.ClientIds == null || !userSession.ClientIds.Contains(clientId))
                {
                    _session.Advanced.Evict(userSession);
                    if (userSession.ClientIds == null)
                        _session.Advanced.Patch<UserSession, List<string>>(userSession.Id, t => t.ClientIds, new List<string> { clientId });
                    else
                        _session.Advanced.Patch<UserSession, string>(userSession.Id, t => t.ClientIds, t => t.Add(clientId));
                    await _session.SaveChangesAsync();
                }
            }
        }
    }
}
