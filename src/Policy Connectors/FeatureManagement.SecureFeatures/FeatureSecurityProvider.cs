using BeyondAuth.PolicyProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace FeatureManagement.SecureFeatures
{
    public class FeatureSecurityProvider : IFeatureSecurityProvider
    {
        private readonly IPolicyProvider _policyProvider;
        private readonly IAuthorizationService _authorizationService;
        private readonly IHttpContextAccessor _contextAccessor;

        public FeatureSecurityProvider(IPolicyProvider policyProvider, IAuthorizationService authorizationService, IHttpContextAccessor contextAccessor)
        {
            _policyProvider = policyProvider;
            _authorizationService = authorizationService;
            _contextAccessor = contextAccessor;
        }

        public async Task<bool> IsAllowedAsync(string feature)
        {
            var policy = _policyProvider.GetFeaturePolicy(feature);
            if (policy == null)
                return true;
            else
            {
                var user = _contextAccessor.HttpContext?.User;
                if (user == null)
                    return false;

                return (await _authorizationService.AuthorizeAsync(user, feature, policy)).Succeeded;
            }
        }

        public async Task<bool> IsAllowedAsync<TContext>(string feature, TContext context)
        {
            var policy = _policyProvider.GetFeaturePolicy(feature);
            if (policy == null)
                return true;

            var user = context is HttpContext ? (context as HttpContext).User : _contextAccessor.HttpContext?.User;
            if (user == null)
                return false;

            return (await _authorizationService.AuthorizeAsync(user, feature, policy)).Succeeded;
        }
    }
}
