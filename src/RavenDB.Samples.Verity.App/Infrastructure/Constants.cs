namespace RavenDB.Samples.Verity.App.Infrastructure
{
    public static class Constants
    {
        public static class EnvVars
        {
            public const string OpenAiApiKey = "SAMPLES_VERITY_OPENAI_API_KEY";
            public const string SecEdgarUserAgent = "SAMPLES_VERITY_SEC_EDGAR_USER_AGENT";

            // Azure Storage – Remote Attachments
            public const string AzureStorageContainer = "SAMPLES_VERITY_AZURE_STORAGE_CONTAINER";
            public const string AzureAccountName = "SAMPLES_VERITY_AZURE_ACCOUNT_NAME";
            public const string AzureAccountKey = "SAMPLES_VERITY_AZURE_ACCOUNT_KEY";
            public const string AzureRemoteFolderName = "SAMPLES_VERITY_AZURE_REMOTE_FOLDER_NAME"; // Optional

            // Azure Storage – Queue ETL
            public const string AzureQueueDefaultEndpointsProtocol = "SAMPLES_VERITY_AZURE_QUEUE_DEFAULT_ENDPOINTS_PROTOCOL";
            public const string AzureQueueEndpointSuffix            = "SAMPLES_VERITY_AZURE_QUEUE_ENDPOINT_SUFFIX";

            // Hub/Sink Replication
            public const string SinkServerUrl        = "SAMPLES_VERITY_SINK_SERVER_URL";
            public const string HubServerInternalUrl = "SAMPLES_VERITY_HUB_SERVER_INTERNAL_URL";

            // Migration
            public const string CommandKey = "CommandKey";
        }
    }
}
