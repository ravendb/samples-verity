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
    string azureStorageQueuesConnectionString,
    string secEdgarUserAgent)
{
    public string OpenAiApiKey { get; } = openAiApiKey;
    public string AzureStorageIdentity { get; } = azureStorageIdentity;
    public string AzureStorageContainer { get; } = azureStorageContainer;
    public string AzureAccountName { get; } = azureAccountName;
    public string AzureAccountKey { get; } = azureAccountKey;
    public string AzureRemoteFolderName { get; } = azureRemoteFolderName;
    public string AzureStorageQueuesConnectionString { get; } = azureStorageQueuesConnectionString;
    public string SecEdgarUserAgent { get; } = secEdgarUserAgent;
}
