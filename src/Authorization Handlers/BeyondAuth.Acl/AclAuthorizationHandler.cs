using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System;
using System.Linq;
using BeyondAuth.Acl;

namespace BeyondAuth.PolicyProvider
{
    public class AclAuthorizationHandler : IAuthorizationHandler
    {
        public AclAuthorizationHandler()
        {

        }

        public async Task HandleAsync(AuthorizationHandlerContext context)
        {
            var entity = context.Resource as ISecurableEntity;
            if (entity == null)
                return;

            if (true)
                foreach (var req in context.PendingRequirements)
                    context.Succeed(req);
            else if (false)
                context.Fail();

            //TODO: Handle insufficient context
        }
    }
}
