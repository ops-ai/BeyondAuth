﻿using System.Collections.Generic;

namespace BeyondAuth.PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Username requirement
    /// </summary>
    public class UsernameRequirement : AuthorizationRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "username";

        /// <summary>
        /// List of usernames the user must match
        /// </summary>
        public List<string> Usernames { get; set; }
    }
}
