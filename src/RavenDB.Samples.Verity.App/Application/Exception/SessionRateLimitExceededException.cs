namespace RavenDB.Samples.Verity.App.Application.Exception;

public class SessionRateLimitExceededException(string message) : System.Exception(message);