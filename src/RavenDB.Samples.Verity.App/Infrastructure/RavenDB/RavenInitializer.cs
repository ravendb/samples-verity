using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.AI;
using Raven.Client.Documents.Operations.ConnectionStrings;
using Raven.Client.Documents.Operations.DataArchival;
using Raven.Client.Documents.Operations.Expiration;
using Raven.Client.Documents.Operations.TimeSeries;
using Raven.Client.Documents.Smuggler;
using RavenDB.Samples.Verity.App.Infrastructure.Tasks;
using System;
using System.Collections.Generic;
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

        private static readonly GenAiConfiguration configuration = new ProfitabilityTask();
        
        private static readonly DataArchivalConfiguration DataArchivalConfig = new()
        {
            Disabled = false,
            ArchiveFrequencyInSec = 60,
            MaxItemsToProcess = 100
        };

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // 1) EXPIRATION
            await store.Maintenance.SendAsync(new ConfigureExpirationOperation(ExpirationConfig), cancellationToken);

            // 2) TIME SERIES
            await store.Maintenance.SendAsync(new ConfigureTimeSeriesOperation(TimeSeriesConfig), cancellationToken);

            // 3) AI CONNECTION STRING
            await store.Maintenance.SendAsync(new PutConnectionStringOperation<AiConnectionString>(AiConnectionStr), cancellationToken);

            // 4) AI AGENT
            //await VerityAgentCreator.Create(store);

            // 5) GEN AI TASK
            //await store.Maintenance.SendAsync(new AddGenAiOperation(configuration), cancellationToken);

            // 6) DATA ARCHIVAL
            await store.Maintenance.SendAsync(new ConfigureDataArchivalOperation(DataArchivalConfig), cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
