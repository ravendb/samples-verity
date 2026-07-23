using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Raven.Client.Documents;
using RavenDB.Samples.Verity.Model;

namespace RavenDB.Samples.Verity.IdentityServer;

// Persists IdentityServer auth events to RavenDB as SecurityEvent documents.
// Provides a queryable compliance audit trail alongside financial data in Verity.
public sealed class RavenEventSink(IDocumentStore store) : IEventSink
{
    public async Task PersistAsync(Event evt)
    {
        string? subjectId = null, username = null, clientId = null, details = null;

        switch (evt)
        {
            case UserLoginSuccessEvent e:
                subjectId = e.SubjectId; username = e.Username; clientId = e.ClientId;
                break;
            case UserLoginFailureEvent e:
                username = e.Username; clientId = e.ClientId; details = e.Message;
                break;
            case UserLogoutSuccessEvent e:
                subjectId = e.SubjectId;
                break;
            case TokenIssuedSuccessEvent e:
                subjectId = e.SubjectId; clientId = e.ClientId; details = $"grant={e.GrantType}";
                break;
            case TokenIssuedFailureEvent e:
                clientId = e.ClientId; details = e.Error;
                break;
            case ClientAuthenticationSuccessEvent e:
                clientId = e.ClientId;
                break;
            case ClientAuthenticationFailureEvent e:
                clientId = e.ClientId; details = e.Message;
                break;
            default:
                details = evt.Message;
                break;
        }

        var securityEvent = new SecurityEvent
        {
            EventType = evt.Name,
            UserId = subjectId is not null ? User.BuildId(subjectId) : null,
            UserName = username,
            ClientId = clientId,
            IpAddress = evt.RemoteIpAddress,
            At = evt.TimeStamp,
            Success = evt.EventType == EventTypes.Success,
            Details = details,
        };

        using var session = store.OpenAsyncSession();
        await session.StoreAsync(securityEvent);
        session.Advanced.GetMetadataFor(securityEvent)["@expires"] =
            DateTime.UtcNow.AddDays(90);
        await session.SaveChangesAsync();
    }
}
