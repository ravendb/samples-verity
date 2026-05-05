namespace RavenDB.Samples.Verity.Model;

public class AuditNotification : IDocument
{
    public static string Collection => "AuditNotifications";

    public string   Id            { get; set; } = null!;
    public string   AuditId       { get; set; } = null!;
    public string   CompanyName   { get; set; } = null!;
    public int      ReportYear    { get; set; }
    public int      ReportQuarter { get; set; }
    public DateTime At            { get; set; }
}
