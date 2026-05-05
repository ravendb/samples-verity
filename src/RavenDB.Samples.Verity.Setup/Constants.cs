namespace RavenDB.Samples.Verity.Setup;

public static class Constants
{
    public const string RemoteAttachmentId       = "verity-azure-storage";
    public const string DatabaseName             = "Verity";
    public const string DatabaseSinkName         = "Verity-sink";
    public const string AzureStorageContainerName = "verity";
    public const string AiConnectionStringName   = "Verity AI Model";

    public static class EnvVars
    {
        public const string OpenAiApiKey                = "SAMPLES_VERITY_OPENAI_API_KEY";
        public const string SecEdgarUserAgent           = "SAMPLES_VERITY_SEC_EDGAR_USER_AGENT";
        public const string AzureStorageConnectionString = "SAMPLES_VERITY_AZURE_STORAGE_CONNECTION_STRING";
        public const string SinkServerUrl               = "SAMPLES_VERITY_SINK_SERVER_URL";
        public const string HubServerInternalUrl        = "SAMPLES_VERITY_HUB_SERVER_INTERNAL_URL";
        public const string CommandKey                  = "CommandKey";
    }

    public static class HttpHeaders
    {
        public const string CommandKey = "X-Command-Key";
    }
}