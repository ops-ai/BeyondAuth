using Microsoft.AspNetCore.Authorization;

namespace AuthorizationPolicyProvider
{
    public class RemoteRequirement : IAuthorizationRequirement
    {
        public RemoteRequirement(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }
}
