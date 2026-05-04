using Raven.Client.Documents;
using Raven.Client.Documents.Operations.ConnectionStrings;
using Raven.Client.Documents.Operations.ETL;
using Raven.Client.Documents.Operations.Replication;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Raven.Migrations;
using RavenDB.Samples.Verity.Model.HubSinks;

namespace RavenDB.Samples.Verity.Setup.Migrations;

[Migration(5)]
public sealed class ConfigureHubSink(MigrationContext context) : Migration
{
    public override void Up()
    {
        try
        {
            DocumentStore.Maintenance.Send(new PutPullReplicationAsHubOperation(VerityReplicationHub.Create()));
        }
        catch (Exception ex) when (ex.Message.Contains("there is already a Hub Pull Replications with that name")) { }

        using var sinkStore = new DocumentStore
        {
            Urls     = [context.SinkServerUrl],
            Database = Constants.DatabaseSinkName
        }.Initialize();

        try
        {
            sinkStore.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(Constants.DatabaseSinkName)));
        }
        catch (Exception ex) when (ex.Message.Contains("already exists")) { }

        var hubUrls = string.IsNullOrEmpty(context.HubServerInternalUrl)
            ? DocumentStore.Urls
            : [context.HubServerInternalUrl];

        sinkStore.Maintenance.Send(new PutConnectionStringOperation<RavenConnectionString>(new RavenConnectionString
        {
            Name                  = VerityReplicationSink.ConnectionStringName,
            Database              = DocumentStore.Database,
            TopologyDiscoveryUrls = hubUrls
        }));

        sinkStore.Maintenance.Send(new UpdatePullReplicationAsSinkOperation(VerityReplicationSink.Create()));
    }

    public override void Down()
    {
        throw new NotSupportedException(
            "Rolling back Hub/Sink replication is not supported. Remove the task and connection string via RavenDB Studio on both nodes.");
    }
}
