using System;

namespace AuthorizationPolicyProvider.Exceptions
{
    [Serializable]
    public class PolicyNotFoundException : Exception
    {
        public PolicyNotFoundException(string name) : base($"Policy not found: {name}")
        {

        }
    }
}
