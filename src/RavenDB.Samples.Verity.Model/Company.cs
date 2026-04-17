namespace RavenDB.Samples.Verity.Model;

public class Company
{
    public string   Id              { get; set; } = null!;
    public string   Name            { get; set; } = null!;
    public string   Cik             { get; set; } = null!;
    public string?  Sic             { get; set; }
    public string?  SicDescription  { get; set; }
    public DateTime FiscalYearStart { get; set; }
}
