namespace RavenDB.Samples.Verity.Model.Agents;

public class VeritySaveAuditArgs
{
    public string ReportId        { get; set; } = "";
    public string AuditorName     { get; set; } = "";
    public string AuditorSurname  { get; set; } = "";
    public string AuditorEmail    { get; set; } = "";
    public string AuditString     { get; set; } = "";
}