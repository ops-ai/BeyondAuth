using BeyondAuth.RelatedDataValidation.Requirements;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace BeyondAuth.RelatedDataValidation
{
    public class RelatedDataAuthorizationHandler : AuthorizationHandler<RelatedDataRequirement>
    {
        private readonly IRelatedDataAuthorizationService _authService;

        public RelatedDataAuthorizationHandler(IRelatedDataAuthorizationService authService) => _authService = authService;

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RelatedDataRequirement requirement)
        {
            if (context.Resource is IRelatedDataEntity && await _authService.ValidateResource((IRelatedDataEntity)context.Resource))
                context.Succeed(requirement);
            else
                context.Fail();
        }
    }
}
