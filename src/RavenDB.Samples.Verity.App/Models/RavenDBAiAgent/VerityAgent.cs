namespace RavenDB.Samples.Verity.App.Models.VerityAgent;

public class VerityReply
{
    public string? Answer { get; set; }
    public List<string> Followups { get; set; } = [];
}

public class VeritySaveAuditArgs
{
    public string ReportId      { get; set; } = "";
    public string AuditorName   { get; set; } = "";
    public string AuditorSurname { get; set; } = "";
    public string AuditorEmail  { get; set; } = "";
    public string AuditString   { get; set; } = "";
}
