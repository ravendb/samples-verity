namespace RavenDB.Samples.Verity.App.Models;
public class ReportPart
{
    public string  Id              { get; set; } = null!;
    public string  ReportId        { get; set; } = null!;
    public string  AccessionNumber { get; set; } = null!;
    public string  FormType        { get; set; } = null!;
    public int     Index           { get; set; }
    public int     Total           { get; set; }
    public string? Analysis        { get; set; }
}
