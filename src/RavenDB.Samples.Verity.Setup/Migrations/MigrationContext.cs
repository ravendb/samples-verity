namespace RavenDB.Samples.Verity.Setup.Migrations;

/// <summary>
/// Configuration context passed to migrations.
/// </summary>
public record MigrationContext(
    string OpenAiApiKey,
    string AzureStorageConnectionString,
    string SecEdgarUserAgent,
    string SinkServerUrl,
    string HubServerInternalUrl);