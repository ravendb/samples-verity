namespace RavenDB.Samples.Verity.AppHost;

public static class DistributedApplicationBuilderExtensions
{
    /// <summary>
    /// Adds a parameter with a random value.
    /// </summary>
    public static IResourceBuilder<ParameterResource> AddParameterWithRandomValue(this IDistributedApplicationBuilder builder, [ResourceName] string name, bool secret = false)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var value = System.Security.Cryptography.RandomNumberGenerator.GetString(chars, 16);

        return builder.AddParameter(name, value, secret: secret);
    }
}