using CommunityToolkit.Aspire.Hosting.RavenDB;
using Projects;
using Raven.Client.Http;
using RavenDB.Samples.Verity.AppHost;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

// Accept self-signed server cert in AppHost health-check calls to RavenDB.
RequestExecutor.RemoteCertificateValidationCallback +=
    (_, _, _, errors) => (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.None;

// Env var names must match Constants.EnvVars in RavenDB.Samples.Verity.Setup
const string envOpenAiApiKey                 = "SAMPLES_VERITY_OPENAI_API_KEY";
const string envSecEdgarUserAgent            = "SAMPLES_VERITY_SEC_EDGAR_USER_AGENT";
const string envAzureStorageConnectionString = "SAMPLES_VERITY_AZURE_STORAGE_CONNECTION_STRING";
const string envSinkServerUrl                = "SAMPLES_VERITY_SINK_SERVER_URL";
const string envHubServerInternalUrl         = "SAMPLES_VERITY_HUB_SERVER_INTERNAL_URL";
const string envServerCertPath               = "SAMPLES_VERITY_SERVER_CERT_PATH";
const string envSinkCertPublicBase64         = "SAMPLES_VERITY_SINK_CERT_PUBLIC_BASE64";
const string envSinkCertPfxBase64            = "SAMPLES_VERITY_SINK_CERT_PFX_BASE64";

// .NET config key read by CommunityToolkit.Aspire.RavenDB.Client (double underscore = ':' separator)
// to locate the client certificate for HTTPS connections to RavenDB.
const string envRavenDbClientCertificatePath = "Aspire__RavenDB__Client__CertificatePath";

var builder = DistributedApplication.CreateBuilder(args);

// Storage
var storage = builder.AddAzureStorage("storage").RunAsEmulator();

// Parameters
var ravenDbLicense = builder
    .AddParameter("ravendb-license", secret: true)
    .WithDescription("Your Developer license formatted as JSON.");

var secEdgarUserAgent = builder
    .AddParameter("sec-edgar-user-agent")
    .WithDescription("The email that will be used to obtain data from SEC Edgar database.");

var openAiApiKey = builder.AddParameter("openai-api-key", secret: true)
    .WithDescription("OpenAI API key");

var azureStorageConnectionString = builder
    .AddParameter("azure-storage-connection-string")
    .WithDescription("Azure Storage Connection string. Used both, for the Remote Attachments and the Azure Storage Queues ETL");

// Sink replication certificate — generated once per AppHost run, injected into seeder.
// Migrations are idempotent so the cert is consumed only on first run.
using var rsa = RSA.Create(2048);
var certReq = new CertificateRequest("CN=VerityReplicationSink", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
certReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
certReq.CertificateExtensions.Add(new X509KeyUsageExtension(
    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: false));
certReq.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
    new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") }, critical: false));
var sinkCert = certReq.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10));
var sinkCertPublicBase64 = Convert.ToBase64String(sinkCert.Export(X509ContentType.Cert));
var sinkCertPfxBase64    = Convert.ToBase64String(sinkCert.Export(X509ContentType.Pfx));

// RavenDB server certificate — regenerated each run (RavenDB data is ephemeral / no volume).
// Mounted read-only into the container; host path is injected into clients for auth.
var certsDir = Path.Combine(AppContext.BaseDirectory, "certs");
Directory.CreateDirectory(certsDir);
var serverCertPath = Path.Combine(certsDir, "server.pfx");
{
    using var serverRsa = RSA.Create(2048);
    var serverCertReq = new CertificateRequest("CN=localhost", serverRsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    var sanBuilder = new SubjectAlternativeNameBuilder();
    sanBuilder.AddDnsName("localhost");
    sanBuilder.AddIpAddress(IPAddress.Loopback);
    serverCertReq.CertificateExtensions.Add(sanBuilder.Build());
    serverCertReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
    // Key Usage and EKU required by RavenDB client certificate validation
    serverCertReq.CertificateExtensions.Add(new X509KeyUsageExtension(
        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: false));
    serverCertReq.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
        new OidCollection { new Oid("1.3.6.1.5.5.7.3.1"), new Oid("1.3.6.1.5.5.7.3.2") }, critical: false));
    // Developer license requires cert validity <= 4 months
    var serverCertCreated = serverCertReq.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddMonths(3));
    File.WriteAllBytes(serverCertPath, serverCertCreated.Export(X509ContentType.Pfx));
}
var serverCertObj = X509CertificateLoader.LoadPkcs12FromFile(serverCertPath, null);

// RavenDB — secured mode required for Hub filtered pull replication (WithFiltering = true)
const int ravenHostPort = 9534;
const int ravenTcpPort = 41350;
var settings = RavenDBServerSettings.Secured(
    $"https://localhost:{ravenHostPort}",       // public URL announced to clients (ConnectionStringExpression)
    "/etc/ravendb/security/server.pfx",         // path INSIDE the container
    null,                                        // no PFX password
    $"https://0.0.0.0:{ravenHostPort}",         // RAVEN_ServerUrl — bind on same port as host mapping
    serverCertObj);                              // cert used by Aspire for health checks
settings.Port = ravenHostPort;
settings.TcpPort = ravenTcpPort;

var ravenDbServer = builder
    .AddRavenDB("RavenDB", settings)
    .WithBindMount(certsDir, "/etc/ravendb/security", isReadOnly: true)
    .WithImage("ravendb/ravendb", "7.2-latest")
    .WithIconName("Database")
    .WithEnvironment("RAVEN_License_Eula_Accepted", "true")
    .WithEnvironment("RAVEN_License", ravenDbLicense);

// The toolkit hardcodes containerPort=443 for secured mode, but RAVEN_ServerUrl now binds
// on ravenHostPort. Override the Aspire endpoint annotation so Docker maps host:9534 →
// container:9534, making localhost:9534 reachable both from the host AND from inside the
// container (where RavenDB performs internal cluster/raft verification after DB creation).
ravenDbServer.Resource.Annotations
    .OfType<EndpointAnnotation>()
    .First(e => e.Name == "https")
    .TargetPort = ravenHostPort;

var db = ravenDbServer.AddDatabase("Verity");
var sink = ravenDbServer.AddDatabase("Verity-sink");

var ravenEndpoint = ravenDbServer.GetEndpoint("https");

// Seeder — runs all migrations then exits 0; gates the backend and subscriptions TUI
var seeder = builder.AddProject<RavenDB_Samples_Verity_Setup>("seeder")
    .WithReference(db).WaitFor(db)
    .WithReference(sink).WaitFor(sink)
    .WithEnvironment(envSecEdgarUserAgent, secEdgarUserAgent)
    .WithEnvironment(envOpenAiApiKey, openAiApiKey)
    .WithEnvironment(envAzureStorageConnectionString, azureStorageConnectionString)
    .WithEnvironment(envSinkServerUrl, ravenEndpoint)
    .WithEnvironment(
        envHubServerInternalUrl,
        ReferenceExpression.Create($"https://localhost:{ravenEndpoint.Property(EndpointProperty.TargetPort)}"))
    .WithEnvironment(envSinkCertPublicBase64, sinkCertPublicBase64)
    .WithEnvironment(envSinkCertPfxBase64, sinkCertPfxBase64)
    // Server cert path on the host — seeder uses it as client cert to authenticate to RavenDB
    .WithEnvironment(envServerCertPath, serverCertPath)
    // Seeder is a generic Host (not WebApplication), so Aspire doesn't auto-populate
    // ASPNETCORE_ENVIRONMENT for it; without DOTNET_ENVIRONMENT the self-signed cert
    // acceptance above never activates and database creation over the secured RavenDB
    // connection fails the TLS handshake.
    .WithEnvironment(envRavenDbClientCertificatePath, serverCertPath)
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development");

// Verity App
var functions = builder.AddAzureFunctionsProject<RavenDB_Samples_Verity_App>("app")
    .WithHostStorage(storage)

    .WithEnvironment("BindingConnection", azureStorageConnectionString)

    .WithReference(db)
    .WaitFor(db)
    .WithReference(sink)
    .WaitFor(sink)

    .WaitForCompletion(seeder)

    .WithEnvironment(envSecEdgarUserAgent, secEdgarUserAgent)
    .WithEnvironment(envOpenAiApiKey, openAiApiKey)

    // Azure Storage – Remote Attachments & Queue ETL
    .WithEnvironment(envAzureStorageConnectionString, azureStorageConnectionString)

    // Hub/Sink Replication
    // SinkServerUrl: how the host-resident App reaches the RavenDB server.
    .WithEnvironment(envSinkServerUrl, ravenEndpoint)
    // HubServerInternalUrl: how the Sink (inside the RavenDB container) reaches the Hub.
    // Hub and Sink share the same RavenDB container, reachable internally on port
    // ravenHostPort (9534) — the toolkit's default 443 was overridden above via the
    // EndpointAnnotation TargetPort.
    .WithEnvironment(
        envHubServerInternalUrl,
        ReferenceExpression.Create($"https://localhost:{ravenEndpoint.Property(EndpointProperty.TargetPort)}"))
    // Same DOTNET_ENVIRONMENT gap as seeder/subscriptions — needed for the Azure Functions
    // host to activate the self-signed cert acceptance callback.
    .WithEnvironment(envRavenDbClientCertificatePath, serverCertPath)
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development");

// DataSubscriptionsApp — interactive Spectre.Console TUI, must run in its own console window
// cmd /c start spawns a detached window so Aspire doesn't capture stdin/stdout
builder.AddExecutable("subscriptions", "cmd", "../RavenDB.Samples.Verity.DataSubscriptionsApp",
        "/c", "start", "Verity Subscriptions", "cmd", "/k", "dotnet run")
    .WithReference(sink)
    .WaitFor(sink)
    .WaitForCompletion(seeder)
    .WithEnvironment(envRavenDbClientCertificatePath, serverCertPath)
    // Unlike AddProject, AddExecutable does not inherit DOTNET_ENVIRONMENT from the AppHost —
    // without it the self-signed cert acceptance in DataSubscriptionsApp/Program.cs never
    // activates and the TLS handshake to the secured RavenDB sink is rejected.
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development");

// Frontend
builder.AddNpmApp("Frontend", "../RavenDB.Samples.Verity.Frontend", "dev")
    .WithReference(functions)
    .WithEnvironment("BROWSER", "none")
    .WithEnvironment("APP_HTTP", functions.GetEndpoint("http"))
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile()
    .WaitFor(functions);

builder.Build().Run();
