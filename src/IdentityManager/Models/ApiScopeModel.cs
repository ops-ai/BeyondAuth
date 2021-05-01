using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IdentityManager.Models
{
    /// <summary>
    /// Api scope configuration
    /// </summary>
    public class ApiScopeModel
    {
        /// <summary>
        /// The unique name of the scope
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Indicates if this scope is enabled. Defaults to true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Display name of the scope.
        /// </summary>
        [Required]
        public string DisplayName { get; set; }

        /// <summary>
        /// Description of the scope.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Show in discovery document
        /// </summary>
        public bool ShowInDiscoveryDocument { get; set; }

        /// <summary>
        /// Identity resource is required
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Emphasize
        /// </summary>
        public bool Emphasize { get; set; }

        /// <summary>
        /// List of accociated user claims that should be included when this resource is
        /// requested.
        /// </summary>
        public ICollection<string> UserClaims { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the custom properties for the resource.
        /// </summary>
        public IDictionary<string, string> Properties { get; set; }
    }
}
