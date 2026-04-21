using Duende.IdentityServer.Models;

namespace RavenDB.Samples.Verity.Identity.Documents;

public static class IdentityCollections
{
    public const string Clients = "IdentityClients";
    public const string IdentityResources = "IdentityResources";
    public const string ApiScopes = "ApiScopes";
    public const string ApiResources = "ApiResources";
    public const string PersistedGrants = "PersistedGrants";
    public const string SigningKeys = "SigningKeys";
    public const string IdentityUsers = "IdentityUsers";
    public const string IdentityEvents = "IdentityEvents";
}

public class ClientDoc
{
    public string Id { get; set; } = "";
    public Client Client { get; set; } = new();

    public static string DocId(string clientId) => $"{IdentityCollections.Clients}/{clientId}";
}

public class IdentityResourceDoc
{
    public string Id { get; set; } = "";
    public IdentityResource Resource { get; set; } = new();

    public static string DocId(string name) => $"{IdentityCollections.IdentityResources}/{name}";
}

public class ApiScopeDoc
{
    public string Id { get; set; } = "";
    public ApiScope Scope { get; set; } = new();

    public static string DocId(string name) => $"{IdentityCollections.ApiScopes}/{name}";
}

public class ApiResourceDoc
{
    public string Id { get; set; } = "";
    public ApiResource Resource { get; set; } = new();

    public static string DocId(string name) => $"{IdentityCollections.ApiResources}/{name}";
}

public class PersistedGrantDoc
{
    public string Id { get; set; } = "";
    public string Key { get; set; } = "";
    public string Type { get; set; } = "";
    public string? SubjectId { get; set; }
    public string? SessionId { get; set; }
    public string ClientId { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? Expiration { get; set; }
    public DateTime? ConsumedTime { get; set; }
    public string Data { get; set; } = "";

    public static string DocId(string key) => $"{IdentityCollections.PersistedGrants}/{key}";

    public static PersistedGrantDoc FromGrant(Duende.IdentityServer.Models.PersistedGrant g) => new()
    {
        Id = DocId(g.Key),
        Key = g.Key,
        Type = g.Type,
        SubjectId = g.SubjectId,
        SessionId = g.SessionId,
        ClientId = g.ClientId,
        Description = g.Description,
        CreationTime = g.CreationTime,
        Expiration = g.Expiration,
        ConsumedTime = g.ConsumedTime,
        Data = g.Data
    };

    public Duende.IdentityServer.Models.PersistedGrant ToGrant() => new()
    {
        Key = Key,
        Type = Type,
        SubjectId = SubjectId,
        SessionId = SessionId,
        ClientId = ClientId,
        Description = Description,
        CreationTime = CreationTime,
        Expiration = Expiration,
        ConsumedTime = ConsumedTime,
        Data = Data
    };
}

public class SigningKeyDoc
{
    public string Id { get; set; } = "";
    public string KeyId { get; set; } = "";
    public string Algorithm { get; set; } = "";
    public DateTime Created { get; set; }
    public string Data { get; set; } = "";
    public bool DataProtected { get; set; }
    public bool IsX509Certificate { get; set; }
    public int Version { get; set; }

    public static string DocId(string id) => $"{IdentityCollections.SigningKeys}/{id}";

    public static SigningKeyDoc FromKey(SerializedKey key) => new()
    {
        Id = DocId(key.Id),
        KeyId = key.Id,
        Algorithm = key.Algorithm,
        Created = key.Created,
        Data = key.Data,
        DataProtected = key.DataProtected,
        IsX509Certificate = key.IsX509Certificate,
        Version = key.Version
    };

    public SerializedKey ToKey() => new()
    {
        Id = KeyId,
        Algorithm = Algorithm,
        Created = Created,
        Data = Data,
        DataProtected = DataProtected,
        IsX509Certificate = IsX509Certificate,
        Version = Version
    };
}

public class IdentityUserDoc
{
    public string Id { get; set; } = "";
    public string SubjectId { get; set; } = "";
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public List<UserClaim> Claims { get; set; } = new();

    public static string DocId(string sub) => $"{IdentityCollections.IdentityUsers}/{sub}";
}

public class UserClaim
{
    public string Type { get; set; } = "";
    public string Value { get; set; } = "";
}

public class IdentityEventDoc
{
    public string Id { get; set; } = "";
    public string EventName { get; set; } = "";
    public int EventId { get; set; }
    public string Category { get; set; } = "";
    public string EventType { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string? SubjectId { get; set; }
    public string? ClientId { get; set; }
    public string? GrantType { get; set; }
    public string? Scopes { get; set; }
    public string? RemoteIpAddress { get; set; }
    public string? ProcessId { get; set; }
    public string? ActivityId { get; set; }
    public string? Message { get; set; }
    public string PayloadJson { get; set; } = "";
}

public class IdentityEventDailyDoc
{
    public string Id { get; set; } = "";
    public DateTime Date { get; set; }
}
