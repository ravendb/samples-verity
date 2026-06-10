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
        try
        {
            DocumentStore.Maintenance.Send(new ConfigureTimeSeriesOperation(new TimeSeriesConfiguration
            {
                Collections =
                {
                    { "ApiUsageSession",       new TimeSeriesCollectionConfiguration() },
                    { "GlobalApiUsageLimiter", new TimeSeriesCollectionConfiguration() }
                }
            }));
        }
        catch (Exception ex) when (ex.Message.Contains("rollup") || ex.Message.Contains("retention") || ex.Message.Contains("license"))
        {
            Console.WriteLine($"[WARN] Time series configuration skipped — not supported by current license: {ex.Message}");
        }

        // AI CONNECTION STRING
        const string connectionName = Constants.AiConnectionStringName;
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
        try
        {
            VerityAgentCreator.Create(DocumentStore, connectionName).GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex.Message.Contains("license") || ex.Message.Contains("AI Agent"))
        {
            Console.WriteLine($"[WARN] AI Agent skipped — not supported by current license: {ex.Message}");
        }

        // GEN AI TASKS
        try
        {
            DocumentStore.Maintenance.Send(new AddGenAiOperation(new ChunkAnalysisTask(connectionName)));
            DocumentStore.Maintenance.Send(new AddGenAiOperation(new ProfitabilityTask(connectionName)));
        }
        catch (Exception ex) when (ex.Message.Contains("license") || ex.Message.Contains("GenAi") || ex.Message.Contains("AI"))
        {
            Console.WriteLine($"[WARN] GenAI tasks skipped — not supported by current license: {ex.Message}");
        }
    }

    public override void Down()
    {
        throw new NotSupportedException(
            "Rolling back AI configuration is not supported. Remove tasks, agent, and connection string via RavenDB Studio.");
    }
}
