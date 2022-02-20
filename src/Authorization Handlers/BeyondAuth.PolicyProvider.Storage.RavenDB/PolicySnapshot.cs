using PolicyServer.Core.Models;

namespace BeyondAuth.PolicyProvider.Storage.RavenDB
{
    public class PolicySnapshot
    {
        public string Id { get; set; }

        public List<PolicyModel> Policies { get; set; }
    }
}