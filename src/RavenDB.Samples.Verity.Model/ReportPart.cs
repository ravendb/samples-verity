namespace RavenDB.Samples.Verity.Model;

public class ReportPart : IDocument
{
    public static string Collection => "ReportParts";

    public static string BuildId(int index, string accessionNumber)
        => $"{Collection}/{index}/{accessionNumber}";

    public const string AttachmentName = "text.htm";

    public string  Id              { get; set; } = null!;
    public string  ReportId        { get; set; } = null!;
    public string  AccessionNumber { get; set; } = null!;
    public string  FormType        { get; set; } = null!;
    public int     Index           { get; set; }
    public int     Total           { get; set; }
    public string? Analysis        { get; set; }
}
