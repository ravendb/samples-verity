using Newtonsoft.Json;
using Raven.Client.Documents.Operations.AI;
using RavenDB.Samples.Verity.Model;

namespace RavenDB.Samples.Verity.Model.Tasks;

public class ChunkAnalysisTask : GenAiConfiguration
{
    public const string TaskName       = "ChunkAnalysis";
    public const string TaskIdentifier = "chunk-analysis";

    public ChunkAnalysisTask(string connectionName)
    {
        Name                 = TaskName;
        Identifier           = TaskIdentifier;
        ConnectionStringName = connectionName;
        Disabled             = false;
        Collection           = ReportPart.Collection;

        GenAiTransformation = new GenAiTransformation
        {
            Script = $@"
if (this.Analysis != null) return;

var text = loadAttachment('{ReportPart.AttachmentName}');
if (!text) return;

ai.genContext({{
    ReportId:   this.ReportId,
    Index:      this.Index,
    Total:      this.Total
}}).withText(text);"
        };

        Prompt =
"You are a financial analyst. You are reading one part (chunk) of a multi-part SEC filing " +
"(10-K annual report or 10-Q quarterly report). " +
"Extract every piece of financial information present in this chunk.\n\n" +
"Return a concise but complete analysis covering:\n" +
"- SECTIONS: which SEC sections appear in this chunk (e.g. ITEM 1, ITEM 7)\n" +
"- FINANCIALS: any revenue, expense, asset, liability, or cash-flow figures with their period\n" +
"- RISKS: key risk factors mentioned (if present)\n" +
"- COMMENTARY: management discussion points and forward-looking statements (if present)\n" +
"- CHANGES: material changes vs. prior period (if present)\n\n" +
"If a category has no information in this chunk write 'n/a'. Keep the total response under 600 words.";

        SampleObject = JsonConvert.SerializeObject(new
        {
            Analysis =
                "SECTIONS: ITEM 1, ITEM 1A\n" +
                "FINANCIALS: Revenue 185.0B USD FY2024 (+3.2% YoY); EBIT 4.1B USD\n" +
                "RISKS: Tariff exposure on imported parts; EV transition costs\n" +
                "COMMENTARY: Management expects recovery in Ford Pro segment in H2 2025\n" +
                "CHANGES: Warranty costs increased 1.2B vs prior year"
        });

        UpdateScript = @"
this.Analysis = $output.Analysis;

var report = load(this.ReportId);
if (report) {
    if (!report.ChunkAnalyses) report.ChunkAnalyses = [];
    report.ChunkAnalyses.push(this.Analysis);
    put(this.ReportId, report);
}";

        MaxConcurrency = 8;
    }
}
