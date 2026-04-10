using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Operations.AI;
using Raven.Client.Documents.Operations.ConnectionStrings;
using Raven.Client.Documents.Operations.DataArchival;
using Raven.Client.Documents.Operations.Expiration;
using Raven.Client.Documents.Operations.Revisions;
using Raven.Client.Documents.Operations.TimeSeries;
using Raven.Client.Documents.Smuggler;
using RavenDB.Samples.Verity.App.Infrastructure.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RavenDB.Samples.Verity.App.Infrastructure.RavenDB
{
    public class RavenInitializer(IDocumentStore store) : IHostedService
    {
        private static readonly TimeSeriesConfiguration TimeSeriesConfig = new()
        {
            Collections =
        {
            { "ApiUsageSession", new TimeSeriesCollectionConfiguration() },
            { "GlobalApiUsageLimiter", new TimeSeriesCollectionConfiguration() }
        }
        };

        private static readonly ExpirationConfiguration ExpirationConfig = new()
        {
            Disabled = false,
            DeleteFrequencyInSec = 60
        };

        private static readonly AiConnectionString AiConnectionStr = new()
        {
            Name = "Verity AI Model",
            ModelType = AiModelType.Chat,
            OpenAiSettings = new OpenAiSettings
            {
                ApiKey = Environment.GetEnvironmentVariable(Constants.EnvVars.OpenAiApiKey),
                Model = "gpt-5-mini",
                Endpoint = "https://api.openai.com/v1",
            }
        };

        private static readonly GenAiConfiguration ProfitabilityTask = new ProfitabilityTask();

        private static readonly string? DumpFilePath =
            Environment.GetEnvironmentVariable(Constants.EnvVars.DumpFilePath);

        private static readonly DataArchivalConfiguration DataArchivalConfig = new()
        {
            Disabled = false,
            ArchiveFrequencyInSec = 60,
            MaxItemsToProcess = 100
        };

        private static readonly RevisionsCollectionConfiguration RevisionsAudits = new()
        {
            Disabled = false,
            PurgeOnDelete = true,
        };

        private static readonly RevisionsConfiguration RevisionsConfig = new()
        {
            Collections = new Dictionary<string, RevisionsCollectionConfiguration>()
            {
                { "Audits", RevisionsAudits }
            }
        };

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // 1) IMPORT FROM DUMP
            if (!string.IsNullOrEmpty(DumpFilePath) && File.Exists(DumpFilePath))
            {
                var stats = await store.Maintenance.SendAsync(new GetStatisticsOperation(), cancellationToken);
                if (stats.CountOfDocuments == 0)
                {
                    var operation = await store.Smuggler.ImportAsync(
                        new DatabaseSmugglerImportOptions(),
                        DumpFilePath,
                        cancellationToken);
                    await operation.WaitForCompletionAsync(token: cancellationToken);
                }
            }

            // 2) EXPIRATION
            await store.Maintenance.SendAsync(new ConfigureExpirationOperation(ExpirationConfig), cancellationToken);

            // 3) TIME SERIES
            await store.Maintenance.SendAsync(new ConfigureTimeSeriesOperation(TimeSeriesConfig), cancellationToken);

            // 4) AI CONNECTION STRING
            await store.Maintenance.SendAsync(new PutConnectionStringOperation<AiConnectionString>(AiConnectionStr), cancellationToken);

            // 5) AI AGENT
            await VerityAgentCreator.Create(store);

            // 6) GEN AI TASKS
            //await store.Maintenance.SendAsync(new AddGenAiOperation(ProfitabilityTask), cancellationToken);

            // 7) DATA ARCHIVAL
            await store.Maintenance.SendAsync(new ConfigureDataArchivalOperation(DataArchivalConfig), cancellationToken);

            // 8) REVISIONS
            await store.Maintenance.SendAsync(new ConfigureRevisionsOperation(RevisionsConfig), cancellationToken);

        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
