using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.AI.Agents;
using RavenDB.Samples.Verity.Model;

namespace RavenDB.Samples.Verity.Model.Agents;

public static class VerityAgentCreator
{
    public const string AgentName       = "Verity Audit Agent";
    public const string AgentIdentifier = "verity-audit-agent";

    public static Task Create(IDocumentStore store, string connectionName)
    {
        return store.AI.CreateAgentAsync(
            new AiAgentConfiguration
            {
                Name                 = AgentName,
                Identifier           = AgentIdentifier,
                ConnectionStringName = connectionName,
                SystemPrompt         = @"You are a financial audit assistant for SEC quarterly and annual reports.

You will receive:
- Information about the auditor (name, surname, email)
- The full text content of a 10-Q or 10-K report

Your task is to produce a professional, concise audit of the report, covering:
1. Revenue and expense trends
2. Profitability assessment (profit or loss, magnitude)
3. Asset position
4. Key risks or anomalies observed in the filing
5. Overall audit opinion

Write the audit in the first person as the auditor.
Be factual and grounded in the report content provided.
Do not speculate beyond what the document states.
Always ask the auditor to confirm before saving.",

                Parameters =
                [
                    new AiAgentParameter("userId",   "ID of the auditor (Users/{Company}/{Name} {Surname})"),
                    new AiAgentParameter("reportId", "ID of the report being audited")
                ],

                SampleObject = JsonConvert.SerializeObject(new VerityReply
                {
                    Answer    = "Structured audit assessment of the report",
                    Followups = ["Would you like to highlight any specific risk?", "Shall I save this audit?"]
                }),

                Queries =
                [
                    new AiAgentToolQuery
                    {
                        Name        = "GetReport",
                        Description = "Retrieve the report metadata and financial summary being audited",
                        Query       = $@"
from {Report.Collection} as r
where id(r) = $reportId
select r.AccessionNumber, r.ReportDate, r.FormType, r.Year, r.Quarter,
       r.Revenues, r.Expenses, r.AssetsValue, r.ProfitLoss, r.Profitable,
       r.Summary, r.Abbreviation",
                        ParametersSampleObject = "{}"
                    },

                    new AiAgentToolQuery
                    {
                        Name        = "GetAuditor",
                        Description = "Retrieve the auditor's name and email from the Users collection",
                        Query       = $@"
from {User.Collection} as u
where id(u) = $userId
select u.Name, u.Surname, u.Email, u.CompanyId",
                        ParametersSampleObject = "{}"
                    },

                    new AiAgentToolQuery
                    {
                        Name        = "GetExistingAudit",
                        Description = "Check whether an audit already exists for this report",
                        Query       = $@"
from {Audit.Collection} as a
where a.ReportId = $reportId
select a.AuditorName, a.AuditorSurname, a.AuditString",
                        ParametersSampleObject = "{}"
                    }
                ],

                Actions =
                [
                    new AiAgentToolAction
                    {
                        Name        = "SaveAudit",
                        Description = "Save the completed audit for this report. Only call after the auditor explicitly confirms.",
                        ParametersSampleObject = JsonConvert.SerializeObject(new VeritySaveAuditArgs
                        {
                            ReportId       = Report.BuildId("ExampleCo", 2025, 1),
                            AuditorName    = "Auditor first name",
                            AuditorSurname = "Auditor last name",
                            AuditorEmail   = "auditor@company.com",
                            AuditString    = "Full audit text written by the agent"
                        })
                    }
                ]
            });
    }
}
