using Raven.Client.Documents.Indexes;
using RavenDB.Samples.Verity.Identity.Documents;

namespace RavenDB.Samples.Verity.Identity.Indexes;

public class PersistedGrants_ByLookup : AbstractIndexCreationTask<PersistedGrantDoc, PersistedGrants_ByLookup.Result>
{
    public class Result
    {
        public string? ClientId { get; set; }
        public string? SubjectId { get; set; }
        public string? SessionId { get; set; }
        public string? Type { get; set; }
        public DateTime? Expiration { get; set; }
    }

    public PersistedGrants_ByLookup()
    {
        Map = grants => from g in grants
                        select new Result
                        {
                            ClientId = g.ClientId,
                            SubjectId = g.SubjectId,
                            SessionId = g.SessionId,
                            Type = g.Type,
                            Expiration = g.Expiration
                        };
    }
}
