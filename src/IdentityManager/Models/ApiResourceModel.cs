using IdentityServer4.Models;
using System.Collections.Generic;

namespace IdentityManager.Models
{
    /// <summary>
    /// Api Resource configuration
    /// </summary>
    public class ApiResourceModel
    {
        /// <summary>
        /// The unique name of the resource.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Indicates if this resource is enabled. Defaults to true.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Display name of the resource.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Description of the resource.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// List of accociated user claims that should be included when this resource is
        /// requested.
        /// </summary>
        public ICollection<string> UserClaims { get; set; } = new List<string>();

        /// <summary>
        /// An API must have at least one scope. Each scope can have different settings.
        /// </summary>
        public ICollection<string> Scopes { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the custom properties for the resource.
        /// </summary>
        public IDictionary<string, string> Properties { get; set; }
    }
}
