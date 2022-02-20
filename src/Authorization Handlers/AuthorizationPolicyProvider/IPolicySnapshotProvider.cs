using PolicyServer.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeyondAuth.PolicyProvider
{
    public interface IPolicySnapshotProvider
    {
        Task SaveSnapshotAsync(List<PolicyModel> policies, CancellationToken ct = default);

        Task<List<PolicyModel>?> GetPoliciesSnapshotAsync(CancellationToken ct = default);
    }
}
