namespace Identity.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class BrandingOptions
    {
        /// <summary>
        /// Logo
        /// </summary>
        public string Logo { get; set; }

        /// <summary>
        /// Logo
        /// </summary>
        public string Favicon { get; set; }

        /// <summary>
        /// Theme
        /// </summary>
        public string Theme { get; set; }

        /// <summary>
        /// Primary Color
        /// </summary>
        public string PrimaryColor { get; set; }

        /// <summary>
        /// Secondary Color
        /// </summary>
        public string SecondaryColor { get; set; }

        /// <summary>
        /// Optional link for help page on login page
        /// </summary>
        public string SupportLink { get; set; }

        /// <summary>
        /// Display text for support link
        /// </summary>
        public string SupportMessage { get; set; }

        /// <summary>
        /// Page title
        /// </summary>
        public string PageTitle { get; set; }
    }
}
