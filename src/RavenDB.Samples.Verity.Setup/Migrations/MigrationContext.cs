namespace RavenDB.Samples.Verity.Setup.Migrations;

/// <summary>
/// Configuration context passed to migrations.
/// </summary>
public record MigrationContext(
    string OpenAiApiKey,
    string AzureStorageConnectionString,
    string SecEdgarUserAgent,
    string SinkServerUrl,
    string HubServerInternalUrl,
    string SinkCertPublicBase64,
    string SinkCertPfxBase64,
    string ServerCertPath);