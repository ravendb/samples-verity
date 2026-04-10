namespace RavenDB.Samples.Verity.App.Models;

/// <summary>
/// Dokument RavenDB przechowywany w kolekcji "Reports".
/// Zawiera załącznik "form10-q.htm" lub "form10-k.htm" przetwarzany przez ProfitabilityTask.
/// </summary>
public class Report
{
    public string  Id              { get; set; } = null!;
    public string  CompanyId       { get; set; } = null!;
    public string  AccessionNumber { get; set; } = null!;
    public string  ReportDate      { get; set; } = null!;
    public string  FilingDate      { get; set; } = null!;
    public int     DaysBetween     { get; set; }
    public int     Quarter         { get; set; }
    public int     Year            { get; set; }
    public string  FormType        { get; set; } = null!;
    public string  SourceUrl       { get; set; } = null!;

    // Wypełniane przez ProfitabilityTask (AI)
    public string? Abbreviation { get; set; }
    public int?    Revenues     { get; set; }
    public int?    Expenses     { get; set; }
    public int?    AssetsValue  { get; set; }
    public int?    ProfitLoss   { get; set; }
    public bool?   Profitable   { get; set; }
    public string? Summary      { get; set; }
}
