using Raven.Client.Documents.Operations.Replication;

namespace RavenDB.Samples.Verity.Model.HubSinks;

public static class VerityReplicationSink
{
    public const string ConnectionStringName = "Verity Hub Connection";
    public const string TaskName             = "VerityHubSink";

    public static PullReplicationAsSink Create() => new()
    {
        Name                 = TaskName,
        HubName              = VerityReplicationHub.HubName,
        ConnectionStringName = ConnectionStringName
    };
}
