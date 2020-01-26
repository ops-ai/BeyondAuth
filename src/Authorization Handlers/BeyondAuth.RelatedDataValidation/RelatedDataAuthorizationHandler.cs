using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System;
using System.Linq;
using BeyondAuth.RelatedDataValidation.Requirements;

namespace BeyondAuth.RelatedDataValidation
{
    public class RelatedDataAuthorizationHandler : AuthorizationHandler<RelatedDataRequirement>
    {
        private readonly IRelatedDataAuthorizationService _authService;

        public RelatedDataAuthorizationHandler(IRelatedDataAuthorizationService authService)
        {
            _authService = authService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RelatedDataRequirement requirement)
        {
            if (!(context.Resource is IRelatedDataEntity))
                return;

            if (await _authService.ValidateResource((IRelatedDataEntity)context.Resource))
                context.Succeed(requirement);
            else
                context.Fail();
        }
    }
}
