namespace RavenDB.Samples.Verity.Model;

public class Company : IDocument
{
    public static string Collection => "Companies";

    public static string BuildId(string name) => $"{Collection}/{name}";

    public string   Id              { get; set; } = null!;
    public string   Name            { get; set; } = null!;
    public string   Cik             { get; set; } = null!;
    public string?  Sic             { get; set; }
    public string?  SicDescription  { get; set; }
    public DateTime FiscalYearStart { get; set; }
}
