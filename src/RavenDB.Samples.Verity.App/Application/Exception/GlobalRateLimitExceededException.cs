namespace RavenDB.Samples.Verity.App.Application.Exception;

public class GlobalRateLimitExceededException(string message) : System.Exception(message);