namespace RavenDB.Samples.Verity.Model;

public class SecurityEvent : IDocument
{
    public static string Collection => "SecurityEvents";

    public string    Id        { get; set; } = null!;
    public string    EventType { get; set; } = null!;
    public string?   UserId    { get; set; }
    public string?   UserName  { get; set; }
    public string?   ClientId  { get; set; }
    public string?   IpAddress { get; set; }
    public DateTime  At        { get; set; }
    public bool      Success   { get; set; }
    public string?   Details   { get; set; }
}