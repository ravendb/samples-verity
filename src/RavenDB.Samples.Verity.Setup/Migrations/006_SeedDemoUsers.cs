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

        var hasher = new PasswordHasher<User>();

        // 3. Stwórz usera
        var subjectId = Guid.NewGuid().ToString();
        var user = new User
        {
            Id = User.BuildId(subjectId),
            SubjectId = subjectId,
            Username = "alice",
            Name = "Alice",
            Surname = "Smith",
            Email = "alice@verity.demo",
            Role = UserRole.Employee,
            CompanyId = companies[0].Id,
        };
        user.PasswordHash = hasher.HashPassword(user, "Demo1234!");

        await session.StoreAsync(user);
        await session.SaveChangesAsync();
    }

    public override void Down()
    {

    }
}