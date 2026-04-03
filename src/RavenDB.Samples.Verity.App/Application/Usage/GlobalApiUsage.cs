namespace RavenDB.Samples.Verity.App.Application.Usage;

public class GlobalApiUsage
{
    public string Id { get; set; } = "GlobalApiUsageLimiter/global";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}