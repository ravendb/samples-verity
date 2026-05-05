namespace RavenDB.Samples.Verity.Model;

public class Audit : IDocument
{
    public static string Collection => "Audits";

    public static string BuildId(Company company, Report report)
        => $"{Collection}/{company.Name}/{report.Year}/Q{report.Quarter}";

    public string Id             { get; set; } = null!;
    public string ReportId       { get; set; } = null!;
    public string AuditorName    { get; set; } = null!;
    public string AuditorSurname { get; set; } = null!;
    public string AuditorEmail   { get; set; } = null!;
    public string AuditString    { get; set; } = null!;
    public bool   GeneratedByAi  { get; set; } = false;
}
