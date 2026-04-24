using Raven.Client.Documents.Operations.AI;
using Raven.Client.Documents.Operations.ConnectionStrings;
using Raven.Client.Documents.Operations.TimeSeries;
using Raven.Migrations;
using RavenDB.Samples.Verity.Model.Agents;
using RavenDB.Samples.Verity.Model.Tasks;

namespace RavenDB.Samples.Verity.Setup.Migrations;

[Migration(3)]
public sealed class ConfigureAi(MigrationContext context) : Migration
{
    public override void Up()
    {
        // TIME SERIES
        DocumentStore.Maintenance.Send(new ConfigureTimeSeriesOperation(new TimeSeriesConfiguration
        {
            Collections =
            {
                { "ApiUsageSession",       new TimeSeriesCollectionConfiguration() },
                { "GlobalApiUsageLimiter", new TimeSeriesCollectionConfiguration() }
            }
        }));

        // AI CONNECTION STRING
        const string connectionName = "Verity AI Model";
        DocumentStore.Maintenance.Send(new PutConnectionStringOperation<AiConnectionString>(new AiConnectionString
        {
            Name           = connectionName,
            ModelType      = AiModelType.Chat,
            OpenAiSettings = new OpenAiSettings
            {
                ApiKey   = context.OpenAiApiKey,
                Model    = "gpt-5-mini",
                Endpoint = "https://api.openai.com/v1"
            }
        }));

        // AI AGENT
        VerityAgentCreator.Create(DocumentStore, connectionName).GetAwaiter().GetResult();

        // GEN AI TASKS
        DocumentStore.Maintenance.Send(new AddGenAiOperation(new ChunkAnalysisTask(connectionName)));
        DocumentStore.Maintenance.Send(new AddGenAiOperation(new ProfitabilityTask(connectionName)));
    }

    public override void Down()
    {
        throw new NotSupportedException(
            "Rolling back AI configuration is not supported. Remove tasks, agent, and connection string via RavenDB Studio.");
    }
}
