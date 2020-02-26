using System;

namespace PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Account lockout requirement
    /// </summary>
    public class LockoutRequirement : AccountRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "lockout";

        /// <summary>
        /// How long the lockout should last for
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Number of attempts that trigger a lockout
        /// </summary>
        public ushort Attempts { get; set; }
    }
}
