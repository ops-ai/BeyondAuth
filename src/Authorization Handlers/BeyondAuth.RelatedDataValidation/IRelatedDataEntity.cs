using System.Collections.Generic;

namespace BeyondAuth.RelatedDataValidation
{
    public interface IRelatedDataEntity
    {
        string Sha256HashCode { get; }

        string RelSha256HashCode { get; }

        Dictionary<string, HashSet<string>> Data { get; }
    }
}
