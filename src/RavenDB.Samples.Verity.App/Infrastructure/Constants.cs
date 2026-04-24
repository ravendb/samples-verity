using System;
using System.Collections.Generic;
using System.Text;

namespace RavenDB.Samples.Verity.App.Infrastructure
{
    public static class Constants
    {
        public static class EnvVars
        {
            public const string MaxGlobalRequestsPer15Minutes = "SAMPLES_VERITY_MAX_GLOBAL_REQUESTS_PER_15_MINUTES";
            public const string MaxSessionRequestsPer30Seconds = "SAMPLES_VERITY_MAX_SESSION_REQUESTS_PER_30_SECONDS";
            public const string OpenAiApiKey = "SAMPLES_VERITY_OPENAI_API_KEY";
            public const string SecEdgarUserAgent = "SAMPLES_VERITY_SEC_EDGAR_USER_AGENT";

            // Azure Storage – Remote Attachments
            public const string AzureStorageIdentity = "SAMPLES_VERITY_AZURE_STORAGE_IDENTITY";
            public const string AzureStorageContainer = "SAMPLES_VERITY_AZURE_STORAGE_CONTAINER";
            public const string AzureAccountName = "SAMPLES_VERITY_AZURE_ACCOUNT_NAME";
            public const string AzureAccountKey = "SAMPLES_VERITY_AZURE_ACCOUNT_KEY";
            public const string AzureRemoteFolderName = "SAMPLES_VERITY_AZURE_REMOTE_FOLDER_NAME"; // Optional

            // Azure Storage – Queue ETL
            public const string AzureQueueDefaultEndpointsProtocol = "SAMPLES_VERITY_AZURE_QUEUE_DEFAULT_ENDPOINTS_PROTOCOL";
            public const string AzureQueueEndpointSuffix            = "SAMPLES_VERITY_AZURE_QUEUE_ENDPOINT_SUFFIX";

            public const string DbName = "SAMPLES_VERITY_DB_NAME";

            // Hub/Sink Replication
            public const string SinkServerUrl        = "SAMPLES_VERITY_SINK_SERVER_URL";
            public const string SinkName             = "SAMPLES_VERITY_SINK_DATABASE_NAME";
            public const string HubServerInternalUrl = "SAMPLES_VERITY_HUB_SERVER_INTERNAL_URL";


            // Migration
            public const string CommandKey = "CommandKey";
        }

        public static class DocumentIds
        {
            public const string GlobalApiUsage = "GlobalApiUsageLimiter/global";
            public static string SessionApiUsage(string sessionId) => $"ApiUsageSession/{sessionId}";
        }

        public static class TimeSeries
        {
            public const string Requests = "Requests";
        }

        public static class Cookies
        {
            public const string SessionId = "samples-verity-session-id";
        }
    }
}
