// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Identity.Core
{
    public class ConsentOptions
    {
        /// <summary>
        /// Offline access is enabled
        /// </summary>
        public bool EnableOfflineAccess { get; set; }

        /// <summary>
        /// Label for offline access
        /// </summary>
        public string OfflineAccessDisplayName { get; set; }

        /// <summary>
        /// Offline access scope description
        /// </summary>
        public string OfflineAccessDescription { get; set; }

        /// <summary>
        /// Must choose one permission error message
        /// </summary>
        public string MustChooseOneErrorMessage { get; set; }

        /// <summary>
        /// Invalid selection error message
        /// </summary>
        public string InvalidSelectionErrorMessage { get; set; }
    }
}
