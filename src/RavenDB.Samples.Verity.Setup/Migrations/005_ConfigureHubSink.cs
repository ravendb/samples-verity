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

        // 2) Certificate injected by AppHost — fail fast if missing so the error is obvious
        var publicCertBase64 = context.SinkCertPublicBase64;
        var pfxBase64 = context.SinkCertPfxBase64;

        if (string.IsNullOrWhiteSpace(publicCertBase64))
            throw new InvalidOperationException("SinkCertPublicBase64 was not injected. Ensure the AppHost generated the sink certificate and passed it as an environment variable.");
        if (string.IsNullOrWhiteSpace(pfxBase64))
            throw new InvalidOperationException("SinkCertPfxBase64 was not injected. Ensure the AppHost generated the sink certificate and passed it as an environment variable.");

        // 3) Register Sink certificate on the Hub with allowed paths
        try
        {
            DocumentStore.Maintenance.Send(new RegisterReplicationHubAccessOperation(
                VerityReplicationHub.HubName,
                new ReplicationHubAccess
                {
                    Name = VerityReplicationSink.AccessName,
                    CertificateBase64 = publicCertBase64,
                    AllowedHubToSinkPaths = VerityReplicationSink.AllowedPaths
                }));
        }
        catch (Exception ex) when (ex.Message.Contains("already") && ex.Message.Contains("access")) { }

        // 4) Create Sink database (if it doesn't exist)
        // ServerCertPath is mandatory in secured mode — the sinkStore needs a trusted client cert for every call.
        if (string.IsNullOrEmpty(context.ServerCertPath) || !File.Exists(context.ServerCertPath))
            throw new InvalidOperationException($"ServerCertPath '{context.ServerCertPath}' was not found. Ensure the AppHost exported the server certificate and passed its path as an environment variable.");
        var serverCert = X509CertificateLoader.LoadPkcs12FromFile(context.ServerCertPath, null);

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

        // 5) Connection string on the Sink pointing to the Hub
        var hubUrls = string.IsNullOrEmpty(context.HubServerInternalUrl)
            ? DocumentStore.Urls
            : [context.HubServerInternalUrl];

        sinkStore.Maintenance.Send(new PutConnectionStringOperation<RavenConnectionString>(new RavenConnectionString
        {
            Name = VerityReplicationSink.ConnectionStringName,
            Database = DocumentStore.Database,
            TopologyDiscoveryUrls = hubUrls
        }));

        // 6) Configure the Sink with the certificate and allowed paths
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
