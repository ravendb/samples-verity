using Newtonsoft.Json;
using Raven.Client.Documents.Operations.AI;
using RavenDB.Samples.Verity.Model;

namespace RavenDB.Samples.Verity.Model.Tasks;

public class ProfitabilityTask : GenAiConfiguration
{
    public const string TaskName       = "ProfitabilityAnalysis";
    public const string TaskIdentifier = "profitability-analysis";

    public ProfitabilityTask(string connectionName)
    {
        Name                 = TaskName;
        Identifier           = TaskIdentifier;
        ConnectionStringName = connectionName;
        Disabled             = false;
        Collection           = Report.Collection;

        GenAiTransformation = new GenAiTransformation
        {
            Script = @"
if (this.ChunkCount == 0){}
else if (!this.ChunkAnalyses || this.ChunkAnalyses.length < this.ChunkCount) return;

var metadata = this['@metadata'];
if (metadata['@archived'] === true) return;
if (metadata['@archive-at']) {
    var archiveAt = new Date(metadata['@archive-at']);
    var today = new Date();
    today.setHours(0, 0, 0, 0);
    if (today >= archiveAt) return;
}

var text = 'unknown';

if (this.ChunkCount == 0) {
    text = loadAttachment(`form${this.FormType}.htm`);
}
else {
    var analyses = [];
    for (var i = 0; i < this.ChunkAnalyses.length; i++) {
        analyses.push('=== Part ' + (i + 1) + ' of ' + this.ChunkCount + ' ===\n' + this.ChunkAnalyses[i]);
        text = analyses.join('\n\n');
    }
}

ai.genContext({ CompanyId: this.CompanyId, Id: id(this) })
  .withText(text);"
        };

        Prompt =
"You are a senior financial analyst. You are receiving pre-processed summaries of " +
"all sections of an SEC filing (10-K or 10-Q), each summarised by a separate analysis step. " +
"Synthesise these partial analyses into a final financial assessment of the company.\n\n" +
"If the same figure appears in multiple parts, use the most complete value. " +
"If values conflict, note it in the Summary.\n\n" +
"Calculate the total revenue and total costs. Assess the asset value. " +
"If a value is in millions or other units, write this abbreviation (e.g., 123 mil → save mil).\n\n" +
"Return:\n" +
"- Revenues: the total Revenues of a given quarter as a number\n" +
"- Expenses: the total Expenses of a given quarter as a number\n" +
"- Assets Value: the total value of Assets as a number\n" +
"- Abbreviation: the unit abbreviation if applicable (e.g., mil for millions, k for thousands), or empty string\n" +
"- Summary: a concise synthesis of the full report (keep under 2048 characters)";

        SampleObject = JsonConvert.SerializeObject(new
        {
            Revenues     = 0,
            Expenses     = 0,
            AssetsVal    = 0,
            Abbreviation = "",
            Profitable   = true,
            Summary      = "Detailed description of the quarter"
        });

        UpdateScript = $@"
this.Revenues     = $output.Revenues;
this.Expenses     = $output.Expenses;
this.AssetsVal    = $output.AssetsVal;
this.Abbreviation = $output.Abbreviation;
this.Summary      = $output.Summary;
this.ProfitLoss   = $output.Revenues - $output.Expenses;
this.Profitable   = $output.Revenues > $output.Expenses;
this.ChunkAnalyses = null;

for (var i = 1; i <= this.ChunkCount; i++) {{
    del('{ReportPart.Collection}/' + i + '/' + this.AccessionNumber);
}}";

        MaxConcurrency = 8;
    }
}
