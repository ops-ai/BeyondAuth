using Microsoft.FeatureManagement;

namespace FeatureManagement.SecureFeatures
{
    public class BeyondAuthFeatureManager : IFeatureManager
    {
        private readonly IFeatureManager _featureManager;

        private readonly IFeatureSecurityProvider _featureSecurityProvider;

        public BeyondAuthFeatureManager(IFeatureManager featureManager, IFeatureSecurityProvider featureSecurityProvider)
        {
            _featureManager = featureManager;
            _featureSecurityProvider = featureSecurityProvider;
        }

        public IAsyncEnumerable<string> GetFeatureNamesAsync() => _featureManager.GetFeatureNamesAsync();

        public async Task<bool> IsEnabledAsync(string feature)
        {
            return await _featureSecurityProvider.IsAllowedAsync(feature) && await _featureManager.IsEnabledAsync(feature);
        }

        public async Task<bool> IsEnabledAsync<TContext>(string feature, TContext context)
        {
            return await _featureSecurityProvider.IsAllowedAsync(feature, context) && await _featureManager.IsEnabledAsync(feature, context);
        }
    }
}