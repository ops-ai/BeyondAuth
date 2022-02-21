using BeyondAuth.PolicyProvider.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BeyondAuth.PolicyServer.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BeyondAuth.PolicyProvider
{
    public interface IPolicyProvider
    {
        AuthorizationPolicy GetAuthorizationPolicy(string policyName);

        AuthorizationPolicy? GetFeaturePolicy(string feature);
    }

    public class BeyondAuthPolicyProvider : IPolicyProvider
    {
        private readonly IOptions<PolicyServerOptions> _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ConcurrentDictionary<string, AuthorizationPolicy> _namedAuthorizationPolicies = new ConcurrentDictionary<string, AuthorizationPolicy>();
        private readonly ConcurrentDictionary<string, AuthorizationPolicy> _authorizationPolicies = new ConcurrentDictionary<string, AuthorizationPolicy>();
        private readonly ConcurrentDictionary<string, PolicyModel> _featurePolicies = new ConcurrentDictionary<string, PolicyModel>();
        private readonly ILogger _logger;
        private readonly BackgroundWorker _backgroundWorker = new BackgroundWorker();
        private readonly IPolicySnapshotProvider? _snapshotProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        public BeyondAuthPolicyProvider(IOptions<PolicyServerOptions> options, IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            _options = options;
            _logger = loggerFactory.CreateLogger<BeyondAuthPolicyProvider>();
            _httpClientFactory = httpClientFactory;
            _snapshotProvider = (IPolicySnapshotProvider?)serviceProvider.GetService(typeof(IPolicySnapshotProvider));

            _backgroundWorker.DoWork += BackgroundWorker_DoWork;
            _backgroundWorker.RunWorkerAsync();
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                await RefreshPolicies(new TimeSpan(0, 0, 2).Ticks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading initial policies");
                try
                {
                    if (_snapshotProvider != null)
                    {
                        var snapshot = await _snapshotProvider.GetPoliciesSnapshotAsync().ConfigureAwait(false);

                        if (snapshot != null)
                            foreach (var policy in snapshot)
                                if (policy.Applicability == BeyondAuth.PolicyServer.Core.Entities.PolicyApplicability.Authorization && policy.Matching == BeyondAuth.PolicyServer.Core.Entities.PolicyMatch.Named)
                                {
                                    var newPolicy = new AuthorizationPolicy(policy.Requirements, policy.AuthenticationSchemes);
                                    _namedAuthorizationPolicies.AddOrUpdate(policy.Name!, newPolicy, (name, def) => def = newPolicy);
                                }
                                else if (policy.Applicability == BeyondAuth.PolicyServer.Core.Entities.PolicyApplicability.Authorization)
                                {
                                    var newPolicy = new AuthorizationPolicy(policy.Requirements, policy.AuthenticationSchemes);
                                    _authorizationPolicies.AddOrUpdate(policy.Id, newPolicy, (name, def) => def = newPolicy);
                                }
                                else if (policy.Applicability == BeyondAuth.PolicyServer.Core.Entities.PolicyApplicability.Feature)
                                    _featurePolicies.AddOrUpdate(policy.Id, policy, (name, def) => def = policy);
                    }
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "Error loading from snapshot");
                }
            }

            while (true)
            {
                await Task.Delay(new TimeSpan(0, 0, 10)).ConfigureAwait(false);
                await RefreshPolicies().ConfigureAwait(false);
            }
        }

        private async Task RefreshPolicies(long? timeout = null)
        {
            try
            {
                using (var httpClient = _httpClientFactory.CreateClient("policyserver"))
                {
                    if (timeout.HasValue)
                        httpClient.Timeout = new TimeSpan(timeout.Value);
                    
                    var newDefinitionsRequest = await httpClient.GetAsync($"policies").ConfigureAwait(false);
                    if (newDefinitionsRequest.StatusCode == System.Net.HttpStatusCode.NotModified)
                        return;

                    newDefinitionsRequest.EnsureSuccessStatusCode();

                    var policiesContent = await newDefinitionsRequest.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var newPolicies = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PolicyModel>>(policiesContent);
                    if (newPolicies == null)
                        return;

                    foreach (var policy in newPolicies)
                        if (policy.Applicability == BeyondAuth.PolicyServer.Core.Entities.PolicyApplicability.Authorization && policy.Matching == BeyondAuth.PolicyServer.Core.Entities.PolicyMatch.Named)
                        {
                            var newPolicy = new AuthorizationPolicy(policy.Requirements, policy.AuthenticationSchemes);
                            _namedAuthorizationPolicies.AddOrUpdate(policy.Name!, newPolicy, (name, def) => def = newPolicy);
                        }
                        else if (policy.Applicability == BeyondAuth.PolicyServer.Core.Entities.PolicyApplicability.Authorization)
                        {
                            var newPolicy = new AuthorizationPolicy(policy.Requirements, policy.AuthenticationSchemes);
                            _authorizationPolicies.AddOrUpdate(policy.Id, newPolicy, (name, def) => def = newPolicy);
                        }
                        else if (policy.Applicability == BeyondAuth.PolicyServer.Core.Entities.PolicyApplicability.Feature)
                            _featurePolicies.AddOrUpdate(policy.Id, policy, (name, def) => def = policy);

                    if (_snapshotProvider != null)
                        await _snapshotProvider.SaveSnapshotAsync(newPolicies).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing policy list");
            }
        }

        public AuthorizationPolicy GetAuthorizationPolicy(string policyName)
        {
            if (!_namedAuthorizationPolicies.ContainsKey(policyName))
                throw new PolicyNotFoundException(policyName);
            else
                return _namedAuthorizationPolicies[policyName];
        }

        public AuthorizationPolicy? GetFeaturePolicy(string feature)
        {
            var applicablePolicies = _featurePolicies.Values.Where(t => t.Criteria!.ContainsValue(feature)).ToList();

            if (!applicablePolicies.Any())
                return null;

            if (applicablePolicies.Count == 1)
                return new AuthorizationPolicy(applicablePolicies.First().Requirements, applicablePolicies.First().AuthenticationSchemes);

            return new AuthorizationPolicy(applicablePolicies.SelectMany(t => t.Requirements).Distinct(), applicablePolicies.SelectMany(t => t.AuthenticationSchemes).Distinct());
        }
    }
}
