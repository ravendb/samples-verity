namespace RavenDB.Samples.Verity.Model.HubSinks;

public static class VerityReplicationSink
{
    public const string ConnectionStringName = "Verity Hub Connection";
    public const string TaskName             = "VerityHubSink";
    public const string AccessName           = "VeritySinkAccess";

    public static readonly string[] AllowedPaths = ["Companies/*", "Reports/*"];
}
