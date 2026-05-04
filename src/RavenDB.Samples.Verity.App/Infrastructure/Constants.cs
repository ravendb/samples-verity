namespace RavenDB.Samples.Verity.App.Infrastructure
{
    public static class Constants
    {
        public static class EnvVars
        {
            public const string OpenAiApiKey = "SAMPLES_VERITY_OPENAI_API_KEY";
            public const string SecEdgarUserAgent = "SAMPLES_VERITY_SEC_EDGAR_USER_AGENT";

            // Azure Storage – Remote Attachments & Queues ETL
            public const string AzureStorageConnectionString = "SAMPLES_VERITY_AZURE_STORAGE_CONNECTION_STRING";

            // Hub/Sink Replication
            public const string SinkServerUrl        = "SAMPLES_VERITY_SINK_SERVER_URL";
            public const string HubServerInternalUrl = "SAMPLES_VERITY_HUB_SERVER_INTERNAL_URL";

            // Migration
            public const string CommandKey = "CommandKey";
        }
    }
}
