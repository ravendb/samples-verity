using Duende.IdentityModel;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using System.Security.Claims;

namespace RavenDB.Samples.Verity.Identity.Stores;

public class RavenDbProfileService(RavenDbUserStore users) : IProfileService
{
    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        var sub = context.Subject.GetSubjectId();
        var user = await users.FindBySubjectAsync(sub);
        if (user is null) return;

        var claims = new List<Claim>
        {
            new Claim(JwtClaimTypes.Subject, user.SubjectId),
            new Claim(JwtClaimTypes.Name, user.DisplayName),
            new Claim(JwtClaimTypes.Email, user.Email),
            new Claim(JwtClaimTypes.PreferredUserName, user.Username)
        };

        foreach (var c in user.Claims)
            claims.Add(new Claim(c.Type, c.Value));

        context.AddRequestedClaims(claims);
    }

    public async Task IsActiveAsync(IsActiveContext context)
    {
        var sub = context.Subject.GetSubjectId();
        var user = await users.FindBySubjectAsync(sub);
        context.IsActive = user is { IsActive: true };
    }
}
