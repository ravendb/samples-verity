namespace RavenDB.Samples.Verity.App.Models;

/// <summary>
/// Dokument RavenDB przechowywany w kolekcji "Reports".
/// Zawiera załącznik "form10-q.htm" przetwarzany przez ProfitabilityTask.
/// </summary>
public class Report
{
    public string   Id                    { get; set; } = null!;
    public string   Company               { get; set; } = null!;
    public string   Cik                   { get; set; } = null!;
    public string   AccessionNumber       { get; set; } = null!;
    public string   FilingDate            { get; set; } = null!;
    public string   ReportDate            { get; set; } = null!;
    public int      DaysBetween           { get; set; }
    public string   FormType              { get; set; } = null!;
    public string   SourceUrl             { get; set; } = null!;

    // Wypełniane przez ProfitabilityTask (AI)
    public double?  ProfitLoss            { get; set; }
    public bool?    Profitable            { get; set; }
    public string?  ProfitabilitySummary  { get; set; }
};