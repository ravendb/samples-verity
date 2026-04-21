using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Raven.Client.Documents;
using RavenDB.Samples.Verity.Identity.Documents;
using System.Text.Json;

namespace RavenDB.Samples.Verity.Identity.Stores;

public class RavenDbEventSink(IDocumentStore store) : IEventSink
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public async Task PersistAsync(Event evt)
    {
        var now = DateTime.UtcNow;
        var todayStr = now.Date.ToString("yyyy-MM-dd");

        using var session = store.OpenAsyncSession();

        var doc = new IdentityEventDoc
        {
            EventName = evt.Name,
            EventId = evt.Id,
            Category = evt.Category,
            EventType = evt.EventType.ToString(),
            Timestamp = evt.TimeStamp,
            RemoteIpAddress = evt.RemoteIpAddress,
            ProcessId = evt.ProcessId.ToString(),
            ActivityId = evt.ActivityId,
            PayloadJson = JsonSerializer.Serialize(evt, evt.GetType(), JsonOpts)
        };

        switch (evt)
        {
            case UserLoginSuccessEvent login:
                doc.SubjectId = login.SubjectId;
                doc.ClientId = login.ClientId;
                doc.Message = $"User '{login.Username}' logged in";
                break;
            case UserLoginFailureEvent failure:
                doc.ClientId = failure.ClientId;
                doc.Message = $"Login failure for '{failure.Username}': {evt.Message}";
                break;
            case TokenIssuedSuccessEvent token:
                doc.SubjectId = token.SubjectId;
                doc.ClientId = token.ClientId;
                doc.GrantType = token.GrantType;
                doc.Scopes = token.Scopes;
                doc.Message = $"Token issued ({token.GrantType})";
                break;
        }

        // Forensic store: append-only document with revisions
        await session.StoreAsync(doc, $"{IdentityCollections.IdentityEvents}/{todayStr}/");

        // Time-series for daily dashboards
        var dailyId = $"IdentityEventDaily/{todayStr}";
        var daily = await session.LoadAsync<IdentityEventDailyDoc>(dailyId);
        if (daily is null)
        {
            daily = new IdentityEventDailyDoc { Id = dailyId, Date = now.Date };
            await session.StoreAsync(daily, dailyId);
        }

        session.TimeSeriesFor(daily, "Events").Append(evt.TimeStamp, [1.0], evt.Name);

        await session.SaveChangesAsync();
    }
}
