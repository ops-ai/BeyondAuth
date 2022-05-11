using BeyondAuth.Acl;
using Finbuckle.MultiTenant;
using Identity.Core;
using Microsoft.AspNetCore.Authorization;

namespace IdentityManager.Extensions
{
    public class TenantAccessAuthorizationHandler : AuthorizationHandler<TenantAuthorizationRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;

        public TenantAccessAuthorizationHandler(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requirement"></param>
        /// <returns></returns>
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantAuthorizationRequirement requirement)
        {
            var tenantSettings = _httpContextAccessor.HttpContext!.GetMultiTenantContext<TenantSetting>()?.TenantInfo!;
            var authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();
            var authResult = await authorizationService.AuthorizeAsync(context.User, tenantSettings, new AclAuthorizationRequirement(requirement.Bitmask));
            if (!authResult.Succeeded)
                context.Fail();
            else
                context.Succeed(requirement);
        }
    }
}
