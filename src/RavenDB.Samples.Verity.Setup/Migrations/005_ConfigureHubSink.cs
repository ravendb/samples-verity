using Raven.Client.Documents;
using Raven.Client.Documents.Operations.ConnectionStrings;
using Raven.Client.Documents.Operations.ETL;
using Raven.Client.Documents.Operations.Replication;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Raven.Migrations;
using RavenDB.Samples.Verity.Model.HubSinks;
using System.Security.Cryptography;
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

        // 2) Wygeneruj self-signed certyfikat dla Sinka
        using var rsa = RSA.Create(2048);
        var certRequest = new CertificateRequest(
            "CN=VerityReplicationSink",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        certRequest.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, true));

        var cert = certRequest.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(10));

        // Klucz publiczny (DER, Base64) — rejestrujemy na Hubie
        var publicCertBase64 = Convert.ToBase64String(cert.Export(X509ContentType.Cert));
        // Pełny certyfikat z kluczem prywatnym (PFX, Base64) — używa Sink
        var pfxBase64 = Convert.ToBase64String(cert.Export(X509ContentType.Pfx));

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
        using var sinkStore = new DocumentStore
        {
            Urls = [context.SinkServerUrl],
            Database = Constants.DatabaseSinkName
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
