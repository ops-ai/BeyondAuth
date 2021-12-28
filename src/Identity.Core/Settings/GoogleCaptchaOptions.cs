namespace Identity.Core.Settings
{
    /// <summary>
    /// Google captcha settings
    /// </summary>
    public class GoogleCaptchaOptions
    {
        /// <summary>
        /// Google reCaptcha site key
        /// </summary>
        public string SiteKey { get; set; }

        /// <summary>
        /// Site Secret
        /// </summary>
        public string Secret { get; set; }
    }
}
