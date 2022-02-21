using Microsoft.Extensions.Options;
using BeyondAuth.PolicyServer.Core.Models;
using Raven.Client.Documents;

namespace BeyondAuth.PolicyProvider.Storage.RavenDB
{
    public class RavenDBPolicySnapshotProvider : IPolicySnapshotProvider
    {
        private readonly IDocumentStore _store;
        private readonly IOptions<PolicySnapshotSettings> _snapshotSettings;

        public RavenDBPolicySnapshotProvider(IDocumentStore store, IOptions<PolicySnapshotSettings> snapshotSettings)
        {
            _store = store;
            _snapshotSettings = snapshotSettings;
        }

        public async Task<List<PolicyModel>?> GetPoliciesSnapshotAsync(CancellationToken ct = default)
        {
            using (var session = _store.OpenAsyncSession())
            {
                var snapshot = await session.LoadAsync<PolicySnapshot>(_snapshotSettings.Value.DocumentName ?? "PolicySnapshots/PolicyServer", ct);
                return snapshot?.Policies;
            }
        }

        public async Task SaveSnapshotAsync(List<PolicyModel> policies, CancellationToken ct = default)
        {
            using (var session = _store.OpenAsyncSession())
            {
                var snapshot = await session.LoadAsync<PolicySnapshot>(_snapshotSettings.Value.DocumentName ?? "PolicySnapshots/PolicyServer", ct);
                if (snapshot == null)
                {
                    snapshot = new PolicySnapshot { Id = _snapshotSettings.Value.DocumentName ?? "PolicySnapshots/PolicyServer", Policies = policies };
                    await session.StoreAsync(snapshot, ct);
                    await session.SaveChangesAsync(ct);
                }
                else if (snapshot.Policies.Count != policies.Count || !snapshot.Policies.SequenceEqual(policies))
                {
                    snapshot.Policies = policies;
                    await session.SaveChangesAsync(ct);
                }
            }
        }
    }
}