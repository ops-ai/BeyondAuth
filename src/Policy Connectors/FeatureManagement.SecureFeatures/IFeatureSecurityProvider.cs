namespace FeatureManagement.SecureFeatures
{
    public interface IFeatureSecurityProvider
    {
        Task<bool> IsAllowedAsync(string feature);

        Task<bool> IsAllowedAsync<TContext>(string feature, TContext context);
    }
}
