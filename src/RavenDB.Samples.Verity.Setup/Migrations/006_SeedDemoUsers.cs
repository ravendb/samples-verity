using Microsoft.AspNetCore.Identity;
using Raven.Migrations;
using RavenDB.Samples.Verity.Model;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents;
namespace RavenDB.Samples.Verity.Setup.Migrations;


[Migration(6)]
public sealed class SeedDemoUsers(MigrationContext context) : Migration
{
    public override void Up()
    {
        RunAsync().GetAwaiter().GetResult();
    }
    private async Task RunAsync()
    {
        using var session = DocumentStore.OpenAsyncSession();

        var companies = await session.Query<Company>()
            .OrderBy(c => c.Name)
            .Take(2)
            .ToListAsync();
        if (companies.Count < 2)
            throw new InvalidOperationException("SeedDemoUsers requires at least 2 companies. Run the company import migration first.");
        var hasher = new PasswordHasher<User>();
        static User Make(Action<User> init)
        {
            var s = Guid.NewGuid().ToString();
            var u = new User { Id = User.BuildId(s), SubjectId = s };
            init(u);
            return u;
        }
        var user = new User[]
        {
            Make(u=> {u.Username = "alice"; u.Name = "Alice"; u.Surname = "Smith"; u.Email = "alice@verity.demo"; u.Role = UserRole.Admin;}),
            Make(u=> {u.Username = "bob"; u.Name = "Bob"; u.Surname = "Johnson"; u.Email = "bob@verity.demo"; u.Role = UserRole.Analyst; u.CompanyIds = [companies[0].Id];}),
            Make(u=> {u.Username = "carol"; u.Name = "Carol"; u.Surname = "Williams"; u.Email = "carol@verity.demo"; u.Role = UserRole.Analyst; u.CompanyIds = [companies[1].Id];}),
            Make(u=> {u.Username = "dave"; u.Name = "Dave"; u.Surname = "Brown"; u.Email = "dave@verity.demo"; u.Role = UserRole.Analyst; u.CompanyIds = [companies[1].Id];}),
            Make(u=> {u.Username = "eve"; u.Name = "Eve"; u.Surname = "Davis"; u.Email = "eve@verity.demo"; u.Role = UserRole.Viewer;}),
        };
        foreach (var u in user)
        {
            u.PasswordHash = hasher.HashPassword(u, "Demo1234!");
            await session.StoreAsync(u);
        }
        await session.SaveChangesAsync();
    }

    public override void Down()
    {
        DocumentStore.Operations.Send(
            new Raven.Client.Documents.Operations.DeleteByQueryOperation(
                new Raven.Client.Documents.Queries.IndexQuery { Query = $"from {User.Collection} where Username in ('alice','bob','carol','dave','eve')" }));
    }
}