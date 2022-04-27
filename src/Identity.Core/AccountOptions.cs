using System;

namespace Identity.Core
{
    public class AccountOptions
    {
        /// <summary>
        /// Allow email + password login
        /// </summary>
        public bool AllowLocalLogin { get; set; }

        /// <summary>
        /// Allow remembering login
        /// </summary>
        public bool AllowRememberLogin { get; set; }

        /// <summary>
        /// Allow resetting password
        /// </summary>
        public bool AllowPasswordReset { get; set; }

        /// <summary>
        /// Remember me cookie lifetime
        /// </summary>
        public TimeSpan RememberMeLoginDuration { get; set; }

        /// <summary>
        /// Default place to redirect to if redirectUrl is not provided
        /// </summary>
        public string DashboardUrl { get; set; }

        /// <summary>
        /// Show logout prompt
        /// </summary>
        public bool ShowLogoutPrompt { get; set; }

        /// <summary>
        /// Redirect automatically after signout
        /// </summary>
        public bool AutomaticRedirectAfterSignOut { get; set; }

        /// <summary>
        /// The windows authentication scheme used
        /// </summary>
        public string WindowsAuthenticationSchemeName { get; set; }

        /// <summary>
        /// if user uses windows auth, should we load the groups from windows
        /// </summary>
        public bool IncludeWindowsGroups { get; set; }

        /// <summary>
        /// Invalid credentials error message
        /// </summary>
        public string InvalidCredentialsErrorMessage { get; set; }

        /// <summary>
        /// Customer support email that should be displayed
        /// </summary>
        public string SupportEmail { get; set; }

        /// <summary>
        /// Customer support link that should be displayed
        /// </summary>
        public string SupportLink { get; set; }

        /// <summary>
        /// Default domain to be appended if only username is entered
        /// </summary>
        public string DefaultDomain { get; set; }

        /// <summary>
        /// Url to provide the user to sign up
        /// </summary>
        public string SignupUrl { get; set; }

        /// <summary>
        /// Message to display before the signup link
        /// </summary>
        public string SignupMessage { get; set; }

        /// <summary>
        /// Text inside the signup anchor
        /// </summary>
        public string SignupText { get; set; }
    }
}
