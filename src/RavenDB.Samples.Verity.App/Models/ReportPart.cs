namespace RavenDB.Samples.Verity.App.Models;

/// <summary>
/// Jeden fragment (chunk) wyczyszczonego pliku HTM z raportu SEC EDGAR.
/// ID: <c>Part/{index}/{accessionNumber}</c>
/// Przetwarzany przez <c>ChunkAnalysisTask</c>, który wypełnia <see cref="Analysis"/>.
/// </summary>
public class ReportPart
{
    public string  Id              { get; set; } = null!;  // Part/{index}/{accessionNumber}
    public string  ReportId        { get; set; } = null!;  // ID dokumentu Report (nadrzędny)
    public string  AccessionNumber { get; set; } = null!;
    public string  FormType        { get; set; } = null!;
    public int     Index           { get; set; }           // 1-based
    public int     Total           { get; set; }
    // Tekst (~200 KB) przechowywany jako attachment "text.htm" na tym dokumencie.
    public string? Analysis        { get; set; }           // wypełniane przez ChunkAnalysisTask
}
