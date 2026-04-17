namespace RavenDB.Samples.Verity.Model;

public class Audit
{
    public string Id             { get; set; } = null!;
    public string ReportId       { get; set; } = null!;
    public string AuditorName    { get; set; } = null!;
    public string AuditorSurname { get; set; } = null!;
    public string AuditorEmail   { get; set; } = null!;
    public string AuditString    { get; set; } = null!;
    public bool   GeneratedByAi  { get; set; } = false;
}
