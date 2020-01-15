﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using PolicyServer.Models;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AuthorizationPolicyProvider.Exceptions;

namespace AuthorizationPolicyProvider
{
    public class AuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        public AuthorizationPolicyProvider(IOptions<AuthorizationOptions> options, IHttpClientFactory httpClientFactory) : base(options)
        {
            _httpClientFactory = httpClientFactory;
        }

        public new async Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            //TODO: if allow local policies

            // check if a local policy is already defined
            var policy = await base.GetPolicyAsync(policyName);

            if (policy == null)
            {
                var policyServerClient = _httpClientFactory.CreateClient("PolicyServer");
                var response = await policyServerClient.GetAsync($"policies/{policyName}");

                if (!response.IsSuccessStatusCode)
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        throw new PolicyNotFoundException(policyName);
                    else
                        throw new HttpRequestException($"Policy not found - {response.StatusCode}");

                var serverPolicy = JsonConvert.DeserializeObject<PolicyModel>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

                policy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(serverPolicy.AuthenticationSchemes.ToArray())
                    .AddRequirements(serverPolicy.Requirements.ToArray())
                    .Build();
            }

            return policy;
        }
    }
}