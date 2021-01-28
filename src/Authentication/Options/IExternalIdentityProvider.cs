namespace Authentication.Options
{
    /// <summary>
    /// 
    /// </summary>
    public interface IExternalIdentityProvider
    {
        /// <summary>
        /// Provider protocol
        /// </summary>
        string Protocol { get; }

        /// <summary>
        /// Provider name
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Provider is enabled
        /// </summary>
        bool Enabled { get; set; }
    }
}
