using Raven.Client.Documents.Operations.ConnectionStrings;
using Raven.Client.Documents.Operations.ETL;
using Raven.Client.Documents.Operations.ETL.Queue;
using Raven.Migrations;
using RavenDB.Samples.Verity.Model.Tasks;

namespace RavenDB.Samples.Verity.Setup.Migrations;

[Migration(4)]
public sealed class ConfigureQueueEtl(MigrationContext context) : Migration
{
    public override void Up()
    {
        DocumentStore.Maintenance.Send(new PutConnectionStringOperation<QueueConnectionString>(new QueueConnectionString
        {
            Name = AuditRevisionQueueEtlTask.ConnectionStringName,
            BrokerType = QueueBrokerType.AzureQueueStorage,
            AzureQueueStorageConnectionSettings = new AzureQueueStorageConnectionSettings
            {
                ConnectionString = context.AzureStorageConnectionString
            }
        }));

        try
        {
            DocumentStore.Maintenance.Send(new AddEtlOperation<QueueConnectionString>(AuditRevisionQueueEtlTask.Create()));
        }
        catch (Exception ex) when (ex.Message.Contains("license") || ex.Message.Contains("Queue ETL"))
        {
            Console.WriteLine($"[WARN] Queue ETL skipped — not supported by current license: {ex.Message}");
        }
    }

    public override void Down()
    {
        throw new NotSupportedException(
            "Rolling back Queue ETL configuration is not supported. Remove the task and connection string via RavenDB Studio.");
    }
}
