using Finbuckle.MultiTenant;
using Identity.Core;
using Identity.Core.Permissions;
using IdentityManager.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdentityManager.Infrastructure
{
    public class TenantAccessFilter : IAsyncResourceFilter
    {
        private readonly IAuthorizationService _authorizationService;

        public TenantAccessFilter(IAuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {

                
            
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var tenantSettings = context.HttpContext.GetMultiTenantContext<TenantSetting>()?.TenantInfo!;
            var authResult = await _authorizationService.AuthorizeAsync(context.HttpContext.User, tenantSettings, new TenantAuthorizationRequirement((ulong)TenantPermissions.Manage));
            if (!authResult.Succeeded)
                context.Result = new JsonResult(new { Error = "Headers Missing" });
        }
    }
}
