using BeyondAuth.Acl;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
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
            var userId = context.User.FindFirst(AclClaimsMap.UserId)?.Value;
            var clientId = context.User.FindFirst(AclClaimsMap.ClientId)?.Value;
            var idp = context.User.FindFirst(AclClaimsMap.IdP)?.Value;
            var groups = context.User.FindAll(AclClaimsMap.Groups).Select(t => t.Value);

            if (userId == null) //Allow client_credentials
                context.Succeed(requirement);
            else if (resource.AclHolder == null)
                context.Fail(new AuthorizationFailureReason(this, "ACL Holder is null"));
            else if (userId == resource.AclHolder?.OwnerId && (resource.AclHolder?.OwnerIdP == null || resource.AclHolder?.OwnerIdP == idp))
                context.Succeed(requirement);
            else if ((resource.AclHolder?.AceEntries.FirstOrDefault(t => t.Subject == userId && (t.IdP == null || t.IdP == idp))?.DenyBits & requirement.Bitmask) == requirement.Bitmask)
                context.Fail();
            else if ((resource.AclHolder?.AceEntries.FirstOrDefault(t => t.Subject == userId && (t.IdP == null || t.IdP == idp))?.AllowBits & requirement.Bitmask) == requirement.Bitmask)
                context.Succeed(requirement);
            else if ((resource.AclHolder?.AceEntries.FirstOrDefault(t => groups.Contains(t.Subject) && (t.IdP == null || t.IdP == idp))?.DenyBits & requirement.Bitmask) == requirement.Bitmask)
                context.Fail();
            else if ((resource.AclHolder?.AceEntries.FirstOrDefault(t => groups.Contains(t.Subject) && (t.IdP == null || t.IdP == idp))?.AllowBits & requirement.Bitmask) == requirement.Bitmask)
                context.Succeed(requirement);
            else
                context.Fail();

            return Task.CompletedTask;
        }
    }
}
