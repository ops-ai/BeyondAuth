using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace AuthorizationPolicyProvider
{
    public class AuthorizationPolicyProvider : IAuthorizationPolicyProvider
    {
        public AuthorizationPolicyProvider()
        {

        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            throw new NotImplementedException();
        }

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync()
        {
            throw new NotImplementedException();
        }

        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            throw new NotImplementedException();
        }
    }
}
