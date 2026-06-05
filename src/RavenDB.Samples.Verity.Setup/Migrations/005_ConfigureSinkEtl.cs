using Raven.Client.Documents.Operations.ConnectionStrings;
using Raven.Client.Documents.Operations.ETL;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Raven.Migrations;
using RavenDB.Samples.Verity.Model.Tasks;

namespace RavenDB.Samples.Verity.Setup.Migrations;

[Migration(5)]
public sealed class ConfigureSinkEtl(MigrationContext context) : Migration
{
    public override void Up()
    {
        try
        {
            DocumentStore.Maintenance.Server.Send(
            new CreateDatabaseOperation(new DatabaseRecord(Constants.DatabaseSinkName)));
        }
        catch (Exception ex) when (ex.Message.Contains("already exists")) { }

        DocumentStore.Maintenance.Send(new PutConnectionStringOperation<RavenConnectionString>(new RavenConnectionString
        {
            Name = VeritySinkEtlTask.ConnectionStringName,
            Database = Constants.DatabaseSinkName,
            TopologyDiscoveryUrls = string.IsNullOrEmpty(context.HubServerInternalUrl)
                ? DocumentStore.Urls
                : [context.HubServerInternalUrl]
        }));

        // 3) ETL na źródle (Verity)
        DocumentStore.Maintenance.Send(new AddEtlOperation<RavenConnectionString>(VeritySinkEtlTask.Create()));
    }

    public override void Down()
    {
        throw new NotSupportedException(
            "Rolling back the Sink ETL is not supported. Remove the ETL task and connection string via RavenDB Studio.");
    }
}
