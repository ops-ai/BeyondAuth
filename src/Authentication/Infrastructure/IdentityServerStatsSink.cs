using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Events;
using IdentityServer4.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
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
        private readonly ConcurrentDictionary<string, int> _stats = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="store"></param>
        /// <param name="identityStoreOptions"></param>
        public IdentityServerStatsSink(ILogger<IdentityServerStatsSink> logger, IDocumentStore store, IOptions<IdentityStoreOptions> identityStoreOptions, IHostApplicationLifetime applicationLifetime)
        {
            _logger = logger ?? throw new ArgumentException("loggerFactory is required", nameof(logger));
            _store = store ?? throw new ArgumentException("store is required", nameof(store));
            _identityStoreOptions = identityStoreOptions;

            var timer = new Timer((s) => SaveStatsAsync().ConfigureAwait(false), null, new TimeSpan(0, 5, 0), new TimeSpan(0, 5, 0));
            applicationLifetime.ApplicationStopping.Register(() => SaveStatsAsync().ConfigureAwait(false).GetAwaiter().GetResult());
        }

        private async Task SaveStatsAsync()
        {
            try
            {
                if (_stats.IsEmpty)
                    return;

                var currentTime = DateTime.UtcNow;

                using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
                {
                    foreach (var stat in _stats)
                    {
                        var statData = stat.Key.Split('|');
                        if (!await session.Advanced.ExistsAsync(statData[0]))
                            continue;

                        if (statData.Length == 2)
                            session.TimeSeriesFor(statData[0], statData[1]).Append(DateTime.UtcNow, stat.Value);
                        else
                            session.TimeSeriesFor(statData[0], statData[1]).Append(DateTime.UtcNow, stat.Value, statData[2]);
                    }

                    try
                    {
                        await session.SaveChangesAsync().ConfigureAwait(false);
                    }
                    catch (Raven.Client.Exceptions.Documents.DocumentDoesNotExistException ex)
                    {
                        _logger.LogWarning(ex, "Saving stats failed due to nonexistent document");
                    }
                }

                _stats.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving stats");
            }
        }

        private async Task AddStatAsync(string docId, string eventName, string tag = null, int increment = 1)
        {
            if (string.IsNullOrEmpty(docId) || docId.EndsWith('/') || string.IsNullOrEmpty(eventName))
                return;

            using (var session = _store.OpenAsyncSession(_identityStoreOptions?.Value.DatabaseName))
            {
                if (!await session.Advanced.ExistsAsync(docId))
                    return;
            }

            var key = new StringBuilder($"{docId}|{eventName}");
            if (!string.IsNullOrEmpty(tag))
                key.Append($"|{tag}");

            int currentValue;
            do
            {
                currentValue = _stats.GetOrAdd(key.ToString(), increment);
            } while (!_stats.TryUpdate(key.ToString(), currentValue + increment, currentValue));
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
                    await AddStatAsync($"ApiResources/{authenticationSuccessEvent.ApiName}", evt.Name);
                    break;
                case ApiAuthenticationFailureEvent apiAuthenticationFailureEvent:
                    await AddStatAsync($"ApiResources/{apiAuthenticationFailureEvent.ApiName}", evt.Name);
                    break;
                case ClientAuthenticationSuccessEvent clientAuthSuccess:
                    await AddStatAsync($"Clients/{clientAuthSuccess.ClientId}", evt.Name);
                    break;
                case ClientAuthenticationFailureEvent clientAuthFailure:
                    await AddStatAsync($"Clients/{clientAuthFailure.ClientId}", evt.Name);
                    break;
                case TokenIssuedSuccessEvent tokenIssuedSuccess:
                    await AddStatAsync($"Clients/{tokenIssuedSuccess.ClientId}", evt.Name, tokenIssuedSuccess.SubjectId ?? tokenIssuedSuccess.ClientId);
                    await AddStatAsync(tokenIssuedSuccess.SubjectId, evt.Name, $"Clients/{tokenIssuedSuccess.ClientId}");
                    break;
                case TokenIssuedFailureEvent tokenIssuedFailure:
                    await AddStatAsync($"Clients/{tokenIssuedFailure.ClientId}", evt.Name, tokenIssuedFailure.SubjectId ?? tokenIssuedFailure.ClientId);
                    await AddStatAsync(tokenIssuedFailure.SubjectId, evt.Name, $"Clients/{tokenIssuedFailure.ClientId}");
                    break;
                case TokenIntrospectionSuccessEvent tokenIntrospectionSuccess:
                    await AddStatAsync($"ApiResources/{tokenIntrospectionSuccess.ApiName}", evt.Name);
                    break;
                case TokenIntrospectionFailureEvent tokenIntrospectionFailure:
                    await AddStatAsync($"ApiResources/{tokenIntrospectionFailure.ApiName}", evt.Name);
                    break;
                case TokenRevokedSuccessEvent tokenRevokedSuccess:
                    await AddStatAsync($"Clients/{tokenRevokedSuccess.ClientId}", evt.Name);
                    break;
                case UserLoginSuccessEvent userLoginSuccess:
                    await AddStatAsync($"Clients/{userLoginSuccess.ClientId}", evt.Name, userLoginSuccess.SubjectId);
                    await AddStatAsync(userLoginSuccess.SubjectId, evt.Name, $"Clients/{userLoginSuccess.ClientId}");
                    break;
                case UserLoginFailureEvent userLoginFailure:
                    await AddStatAsync($"Clients/{userLoginFailure.ClientId}", evt.Name);
                    break;
                case UserLogoutSuccessEvent userLogoutSuccess:
                    await AddStatAsync(userLogoutSuccess.SubjectId, evt.Name);
                    break;
                case ConsentGrantedEvent consentGranted:
                    await AddStatAsync($"Clients/{consentGranted.ClientId}", evt.Name, consentGranted.SubjectId);
                    await AddStatAsync(consentGranted.SubjectId, evt.Name, $"Clients/{consentGranted.ClientId}");
                    break;
                case ConsentDeniedEvent consentDenied:
                    await AddStatAsync($"Clients/{consentDenied.ClientId}", evt.Name, consentDenied.SubjectId);
                    await AddStatAsync(consentDenied.SubjectId, evt.Name, $"Clients/{consentDenied.ClientId}");
                    break;
                case DeviceAuthorizationSuccessEvent deviceAuthorizationSuccess:
                    await AddStatAsync($"Clients/{deviceAuthorizationSuccess.ClientId}", evt.Name);
                    break;
                case DeviceAuthorizationFailureEvent deviceAuthorizationFailure:
                    await AddStatAsync($"Clients/{deviceAuthorizationFailure.ClientId}", evt.Name);
                    break;
            }
        }
    }
}
