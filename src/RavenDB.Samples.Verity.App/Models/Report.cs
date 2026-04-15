namespace RavenDB.Samples.Verity.App.Models;

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

    public int ChunkCount { get; set; }

    public List<string>? ChunkAnalyses { get; set; }

    // ── AI-synthesised fields (written by ProfitabilityTask) ─────────────────

    public int?    Revenues     { get; set; }
    public int?    Expenses     { get; set; }
    public int?    AssetsVal    { get; set; }
    public int?    ProfitLoss   { get; set; }
    public string? Abbreviation { get; set; }
    public string? Summary      { get; set; }
    public bool?   Profitable   { get; set; }
}
