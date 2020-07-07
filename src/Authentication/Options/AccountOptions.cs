// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;

namespace Authentication.Models.Account
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
    }
}
