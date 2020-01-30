using Raven.Client.Documents.Indexes;

namespace BeyondAuth.RelatedDataValidation.Indices
{
    public class Index_RelatedDataAgg : AbstractIndexCreationTask<RelatedDataEntity>
    {
        public override string IndexName => "RelatedDataAgg";

        public override IndexDefinition CreateIndexDefinition()
        {
            return new IndexDefinition
            {
                Maps =
                {
                    @"from r in docs.RelatedDataEntities
                    from d in r.Data
                    select new
                    {
                        Hashes = new [] { r.Sha256HashCode, r.RelSha256HashCode },
                        d.Key,
                        d.Value
                    }"
                },
                Reduce = @"from r in results
                    group r by r.Key into g
                    select new
                    {
                        Key = g.Key,
                        Hashes = g.SelectMany(x => x.Hashes).Where(x => x != null).Distinct(),
                        Value = g.SelectMany(x => x.Value).Distinct()
                    }
                    "
            };
        }
    }
}
