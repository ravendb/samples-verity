using Raven.Client.Documents.Attachments;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Operations.Attachments.Remote;
using Raven.Client.Documents.Operations.DataArchival;
using Raven.Client.Documents.Operations.Expiration;
using Raven.Client.Documents.Operations.Revisions;
using Raven.Client.ServerWide.Operations;
using Raven.Client.ServerWide.Operations.Configuration;
using Raven.Migrations;
using RavenDB.Samples.Verity.Model;
using RavenDB.Samples.Verity.Model.Subscriptions;

namespace RavenDB.Samples.Verity.Setup.Migrations;

[Migration(1)]
public sealed class ConfigureDatabase(MigrationContext context) : Migration
{
    public override void Up()
    {
        // 0) DATABASE SETTINGS
        DocumentStore.Maintenance.Send(new PutDatabaseSettingsOperation(DocumentStore.Database,
            new Dictionary<string, string>
            {
                ["Ai.GenAi.GenAiSendToModelTimeoutInSec"] = "180"
            }));

        DocumentStore.Maintenance.Server.Send(new ToggleDatabasesStateOperation(DocumentStore.Database, true));
        DocumentStore.Maintenance.Server.Send(new ToggleDatabasesStateOperation(DocumentStore.Database, false));

        // 1) REMOTE ATTACHMENTS
        var parsed = context.AzureStorageConnectionString
            .Split(";", StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split("="))
            .ToDictionary(pair => pair[0], pair => pair[1]);

        var remoteAttachmentsConfig = new RemoteAttachmentsConfiguration
        {
            Destinations = new Dictionary<string, RemoteAttachmentsDestinationConfiguration>
            {
                {
                    Constants.RemoteAttachmentId,
                    new RemoteAttachmentsDestinationConfiguration
                    {
                        AzureSettings = new RemoteAttachmentsAzureSettings
                        {
                            StorageContainer = Constants.AzureStorageContainerName,
                            AccountName      = parsed["AccountName"],
                            AccountKey       = parsed["AccountKey"],
                        },
                        Disabled = false
                    }
                }
            },
            CheckFrequencyInSec = 6000,
            MaxItemsToProcess   = 25,
            ConcurrentUploads   = 5
        };

        DocumentStore.Maintenance.Send(new ConfigureRemoteAttachmentsOperation(remoteAttachmentsConfig));

        // 2) DATA ARCHIVAL
        DocumentStore.Maintenance.Send(new ConfigureDataArchivalOperation(new DataArchivalConfiguration
        {
            Disabled              = false,
            ArchiveFrequencyInSec = 60,
            MaxItemsToProcess     = 100
        }));

        // 3) REVISIONS
        DocumentStore.Maintenance.Send(new ConfigureRevisionsOperation(new RevisionsConfiguration
        {
            Collections = new Dictionary<string, RevisionsCollectionConfiguration>
            {
                {
                    Audit.Collection, new RevisionsCollectionConfiguration
                    {
                        Disabled      = false,
                        PurgeOnDelete = true
                    }
                }
            }
        }));

        // 4) EXPIRATION
        DocumentStore.Maintenance.Send(new ConfigureExpirationOperation(new ExpirationConfiguration
        {
            Disabled             = false,
            DeleteFrequencyInSec = 60
        }));

        // 5) SUBSCRIPTIONS
        try
        {
            CompaniesSubscription.Create(DocumentStore).GetAwaiter().GetResult();
        }
        catch (Exception) { }

    }

    public override void Down()
    {
        DocumentStore.Maintenance.Send(new ConfigureRemoteAttachmentsOperation(
            new RemoteAttachmentsConfiguration { Destinations = new Dictionary<string, RemoteAttachmentsDestinationConfiguration>() }));

        DocumentStore.Maintenance.Send(new ConfigureDataArchivalOperation(
            new DataArchivalConfiguration { Disabled = true }));

        DocumentStore.Maintenance.Send(new ConfigureRevisionsOperation(
            new RevisionsConfiguration()));

        DocumentStore.Maintenance.Send(new ConfigureExpirationOperation(
            new ExpirationConfiguration { Disabled = true }));
    }
}
