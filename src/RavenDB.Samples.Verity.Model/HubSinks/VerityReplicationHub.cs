using Raven.Client.Documents.Operations.Replication;

namespace RavenDB.Samples.Verity.Model.HubSinks;

public static class VerityReplicationHub
{
    public const string HubName = "VerityReplicationHub";

    public static PullReplicationDefinition Create() => new()
    {
        Name          = HubName,
        WithFiltering = false,
        Mode          = PullReplicationMode.HubToSink
    };
}
