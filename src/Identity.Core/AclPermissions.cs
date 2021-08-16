using BeyondAuth.Acl;

namespace Identity.Core
{
    /// <summary>
    /// ACL Permission mapper
    /// </summary>
    public static class AclPermissions
    {
        public static AclAuthorizationRequirement Open = new AclAuthorizationRequirement { Name = nameof(Open), Bitmask = 1 };

        public static AclAuthorizationRequirement Read = new AclAuthorizationRequirement { Name = nameof(Read), Bitmask = 2 };

        public static AclAuthorizationRequirement List = new AclAuthorizationRequirement { Name = nameof(List), Bitmask = 4 };

        public static AclAuthorizationRequirement Write = new AclAuthorizationRequirement { Name = nameof(Write), Bitmask = 8 };

        public static AclAuthorizationRequirement Download = new AclAuthorizationRequirement { Name = nameof(Download), Bitmask = 16 };

        public static AclAuthorizationRequirement Delete = new AclAuthorizationRequirement { Name = nameof(Delete), Bitmask = 32 };
    }
}
