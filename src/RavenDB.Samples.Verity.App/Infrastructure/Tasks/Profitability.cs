using Newtonsoft.Json;
using Raven.Client.Documents.Operations.AI;

namespace RavenDB.Samples.Verity.App.Infrastructure.Tasks
{
    public class ProfitabilityTask : GenAiConfiguration
    {
        public ProfitabilityTask()
        {
            Name = "ProfitabilityAnalysis";
            Identifier = "profitability-analysis";
            ConnectionStringName = "Verity AI Model";
            Disabled = false;
            Collection = "Reports";

            GenAiTransformation = new GenAiTransformation
            {
                Script = @"
var metadata = this['@metadata'];
if (metadata['@archive-at']) {
    var archiveAt = new Date(metadata['@archive-at']);
    var today = new Date();
    today.setHours(0, 0, 0, 0);
}
if (today < archiveAt) {
    if (this.FormType == ""10-Q""){
        const text = loadAttachment(`form10-q.htm`);
        if (text !== null) {
            ai.genContext({CompanyId: this.CompanyId, Id: id(this)})
            .withText(loadAttachment(`form10-q.htm`));
        }
    }
    else if (this.FormType == ""10-K""){
        const text = loadAttachment(`form10-k.htm`);
        if (text !== null) {
            ai.genContext({CompanyId: this.CompanyId, Id: id(this)})
            .withText(loadAttachment(`form10-k.htm`));
        }
    }
}"
            };

            Prompt = 
"Analyze the attached quarterly report document and assess its financial performance." +
"Calculate the total revenue and total costs." +
"Assess the asset value." +
"If a value is in millions or other units, write this abbreviation (e.g., 123 mil save mil)." +
"Return:" +
"- Revenues: the total Revenues as a number" +
"- Expenses: the total expenses as a number" +
"- Assets Value: the total value of assets as a number" +
"- Abbreviation: the unit abbreviation if applicable (e.g., mil for millions, k for thousands), or empty string if not applicable" +
"- Summary: a detailed quarterly report (keep under 512 characters)";

            SampleObject = JsonConvert.SerializeObject(new{
                    Revenues = 0,
                    Expenses = 0,
                    AssetsVal = 0,
                    Abbreviation = "",
                    Profitable = true,
                    Summary = "Detailed description of the quarter"
            });

            UpdateScript = @"
                this.Revenues = $output.Revenues;
                this.Expenses = $output.Expenses;
                this.AssetsValue = $output.AssetsVal;
                this.ProfitLoss = $output.Revenues - $output.Expenses;
                this.Profitable = this.ProfitLoss > 0;
                this.Summary = $output.Summary;
                if ($output.Abbreviation) {
                    this.Abbreviation = $output.Abbreviation;
                }
                ";

            MaxConcurrency = 4;
        }
    }
}