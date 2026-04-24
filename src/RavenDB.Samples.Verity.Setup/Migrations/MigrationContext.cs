namespace RavenDB.Samples.Verity.Setup.Migrations;

/// <summary>
/// Configuration context passed to migrations.
/// </summary>
public class MigrationContext(
    string openAiApiKey,
    string azureStorageIdentity,
    string azureStorageContainer,
    string azureAccountName,
    string azureAccountKey,
    string azureRemoteFolderName,
    string azureQueueDefaultEndpointsProtocol,
    string azureQueueEndpointSuffix,
    string secEdgarUserAgent,
    string sinkServerUrl,
    string sinkName,
    string hubServerInternalUrl)
{
    public string OpenAiApiKey { get; } = openAiApiKey;
    public string AzureStorageIdentity { get; } = azureStorageIdentity;
    public string AzureStorageContainer { get; } = azureStorageContainer;
    public string AzureAccountName { get; } = azureAccountName;
    public string AzureAccountKey { get; } = azureAccountKey;
    public string AzureRemoteFolderName { get; } = azureRemoteFolderName;
    public string AzureQueueDefaultEndpointsProtocol { get; } = azureQueueDefaultEndpointsProtocol;
    public string AzureQueueEndpointSuffix           { get; } = azureQueueEndpointSuffix;
    public string SecEdgarUserAgent { get; } = secEdgarUserAgent;
    public string SinkServerUrl { get; } = sinkServerUrl;
    public string SinkName      { get; } = sinkName;
    public string HubServerInternalUrl { get; } = hubServerInternalUrl;
}
