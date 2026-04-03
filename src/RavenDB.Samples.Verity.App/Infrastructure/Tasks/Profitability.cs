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
const [day, month, year] = this.ReportDate.split(""."");
const date = new Date(year, month - 1, day); // miesiące 0-based
const dateNow = new Date();
const monthsBetween = (dateNow.getMonth() - date.getMonth()) + ((dateNow.getFullYear() - date.getFullYear()) * 12);
if (monthsBetween <= 12) {
    const text = loadAttachment(`form10-q.htm`);
    if (text !== null) {
        ai.genContext({
            Company: this.Company, Id: id(this)
        })
        .withText(loadAttachment(`form10-q.htm`));
    }
}"
            };

            Prompt = "Analyze the attached quaterly reports document and assess its financial outcome." +
                    "Calculate the total revenue and total costs." +
                    "Determine whether the transaction resulted in a net profit or a net loss." +
                    "Return:" +
                    "- ProfitLoss: the net amount (positive = profit, negative = loss) as a number" +
                    "- Profitable: true if ProfitLoss > 0, otherwise false" +
                    "- Summary: a one-sentence description of the financial quarter";

            SampleObject = JsonConvert.SerializeObject(new{
                    ProfitLoss = 0.0,
                    Profitable = true,
                    Summary = "Brief description of the financial outcome"
                });

            UpdateScript = @"
                this.ProfitLoss = $output.ProfitLoss;
                this.Profitable = $output.Profitable;
                this.ProfitabilitySummary = $output.Summary;
                ";

            MaxConcurrency = 4;
        }
    }
}