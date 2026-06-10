using Raven.Client.Documents;
using Raven.Client.Documents.Operations.ConnectionStrings;
using Raven.Client.Documents.Operations.ETL;
using Raven.Client.Documents.Operations.Replication;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Raven.Migrations;
using RavenDB.Samples.Verity.Model.HubSinks;
using System.Security.Cryptography.X509Certificates;

namespace RavenDB.Samples.Verity.Setup.Migrations;

[Migration(5)]
public sealed class ConfigureHubSink(MigrationContext context) : Migration
{
    public override void Up()
    {
        // 1) Create Hub with filtering
        try
        {
            DocumentStore.Maintenance.Send(new PutPullReplicationAsHubOperation(VerityReplicationHub.Create()));
        }
        catch (Exception ex) when (ex.Message.Contains("there is already a Hub Pull Replications with that name")) { }

        // 2) Certyfikat wstrzyknięty przez AppHost
        var publicCertBase64 = context.SinkCertPublicBase64;
        var pfxBase64 = context.SinkCertPfxBase64;

        // 3) Zarejestruj certyfikat Sinka na Hubie z dozwolonymi ścieżkami
        DocumentStore.Maintenance.Send(new RegisterReplicationHubAccessOperation(
            VerityReplicationHub.HubName,
            new ReplicationHubAccess
            {
                Name = VerityReplicationSink.AccessName,
                CertificateBase64 = publicCertBase64,
                AllowedHubToSinkPaths = VerityReplicationSink.AllowedPaths
            }));

        // 4) Utwórz bazę Sink (jeśli nie istnieje)
        var serverCert = !string.IsNullOrEmpty(context.ServerCertPath) && File.Exists(context.ServerCertPath)
            ? X509CertificateLoader.LoadPkcs12FromFile(context.ServerCertPath, null)
            : null;

        using var sinkStore = new DocumentStore
        {
            Urls = [context.SinkServerUrl],
            Database = Constants.DatabaseSinkName,
            Certificate = serverCert
        }.Initialize();

        try
        {
            sinkStore.Maintenance.Server.Send(
                new CreateDatabaseOperation(new DatabaseRecord(Constants.DatabaseSinkName)));
        }
        catch (Exception ex) when (ex.Message.Contains("already exists")) { }

        // 5) Connection string na Sinku wskazujący na Hub
        var hubUrls = string.IsNullOrEmpty(context.HubServerInternalUrl)
            ? DocumentStore.Urls
            : [context.HubServerInternalUrl];

        sinkStore.Maintenance.Send(new PutConnectionStringOperation<RavenConnectionString>(new RavenConnectionString
        {
            Name = VerityReplicationSink.ConnectionStringName,
            Database = DocumentStore.Database,
            TopologyDiscoveryUrls = hubUrls
        }));

        // 6) Skonfiguruj Sink z certyfikatem i dozwolonymi ścieżkami
        sinkStore.Maintenance.Send(new UpdatePullReplicationAsSinkOperation(new PullReplicationAsSink
        {
            Name = VerityReplicationSink.TaskName,
            HubName = VerityReplicationHub.HubName,
            ConnectionStringName = VerityReplicationSink.ConnectionStringName,
            CertificateWithPrivateKey = pfxBase64,
            AllowedHubToSinkPaths = VerityReplicationSink.AllowedPaths
        }));
    }

    public override void Down()
    {
        throw new NotSupportedException(
            "Rolling back Hub/Sink replication is not supported. Remove the task and connection string via RavenDB Studio on both nodes.");
    }
}
