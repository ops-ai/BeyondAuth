using System;
using System.Collections.Generic;

namespace BeyondAuth.RelatedDataValidation
{
    public class RelatedDataEntity : IRelatedDataEntity
    {
        /// <summary>
        /// SHA-256 hash of the file / resource
        /// </summary>
        public string Sha256HashCode { get; set; }

        /// <summary>
        /// SHA-256 hash of the file / resource this was derived from
        /// </summary>
        public string RelSha256HashCode { get; set; }

        /// <summary>
        /// List of other properties and values to store
        /// </summary>
        public Dictionary<string, HashSet<string>> Data { get; set; } = new Dictionary<string, HashSet<string>>();
    }
}
