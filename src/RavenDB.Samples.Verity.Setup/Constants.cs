namespace RavenDB.Samples.Verity.Setup;

public static class Constants
{
    /// <summary>
    /// The identifier of the remote attachments configuration <see cref="https://docs.ravendb.net/7.2/document-extensions/attachments/configure-remote-attachments"/>.
    /// </summary>
    public const string RemoteAttachmentId = "verity-azure-storage";
    
    /// <summary>
    /// The name of the RavenDB database used by this sample.
    /// </summary>
    public const string DatabaseName = "Verity";

    /// <summary>
    /// The name of the sink database.
    /// </summary>
    public const string DatabaseSinkName = "Verity-sink";

    /// <summary>
    /// The remote attachments container name
    /// </summary>
    public const string AzureStorageContainerName = "verity";
}