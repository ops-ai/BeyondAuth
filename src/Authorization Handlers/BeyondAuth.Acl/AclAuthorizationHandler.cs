using BeyondAuth.Acl;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace BeyondAuth.PolicyProvider
{
    public class AclAuthorizationHandler : AuthorizationHandler<AclAuthorizationRequirement, ISecurableEntity>
    {
        public AclAuthorizationHandler()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requirement"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AclAuthorizationRequirement requirement, ISecurableEntity resource)
        {
            if (context.User.Identity.Name == resource.AclHolder?.OwnerId)
                context.Succeed(requirement);
            else if (resource.AclHolder?.AceEntries.FirstOrDefault(t => t.Subject == context.User.Identity.Name)?.AllowBits % requirement.Bitmask == requirement.Bitmask)
                context.Succeed(requirement);
            else
                context.Fail();

            return Task.CompletedTask;
        }
    }
}
