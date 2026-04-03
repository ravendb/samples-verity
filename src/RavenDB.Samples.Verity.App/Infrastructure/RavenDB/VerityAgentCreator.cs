using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.AI.Agents;
using RavenDB.Samples.Verity.App.Models.VerityAgent;

namespace RavenDB.Samples.Verity.App.Infrastructure.RavenDB;

public static class VerityAgentCreator
{
    public static Task Create(IDocumentStore store)
    {
        return store.AI.CreateAgentAsync(
            new AiAgentConfiguration
            {
                Name = "Verity Assistant",
                Identifier = "verity-assistant",
                ConnectionStringName = "Verity's AI Model",
                SystemPrompt = @"You are an invoice processing assistant.
You help users extract and analyze data from invoice documents.

When invoice text is provided, extract:
- Document date
- Invoice number
- Vendor / supplier name
- Total amount and currency
- Line items (description, quantity, unit price)
- Due date
- VAT / tax amount

Present a clear structured summary of the extracted data.
Always ask the user to confirm before saving any data.
Do NOT discuss topics unrelated to invoices.",
                Parameters =
                [
                    new AiAgentParameter("userId", "ID of the user processing the invoice")
                ],
                SampleObject = JsonConvert.SerializeObject(new VerityReply
                {
                    Answer = "Structured answer about the invoice",
                    Followups = ["Likely follow-up questions"]
                }),
                Queries =
                [
                    new AiAgentToolQuery
                    {
                        Name        = "FindInvoices",
                        Description = "Semantic search for existing invoices",
                        Query       = @"
from Invoices
where UserId = $userId
    and (vector.search(embedding.text(VendorName), $query) or vector.search(embedding.text(InvoiceNumber), $query))
order by InvoiceDate desc
limit 5",
                        ParametersSampleObject = "{\"query\": [\"search terms to find matching invoice\"]}"
                    },
                    new AiAgentToolQuery
                    {
                        Name        = "GetInvoicesByDateRange",
                        Description = "Retrieve invoices within a given date range",
                        Query       = @"
from Invoices
where UserId = $userId
    and InvoiceDate between $startDate and $endDate
order by InvoiceDate desc",
                        ParametersSampleObject = "{\"startDate\": \"yyyy-MM-dd\", \"endDate\": \"yyyy-MM-dd\"}"
                    },
                    new AiAgentToolQuery
                    {
                        Name        = "GetInvoicesByVendor",
                        Description = "Retrieve all invoices from a specific vendor",
                        Query       = @"
from Invoices
where UserId = $userId
    and VendorName = $vendorName
order by InvoiceDate desc
limit 10",
                        ParametersSampleObject = "{\"vendorName\": \"Vendor name\"}"
                    },
                ],
                Actions =
                [
                    new AiAgentToolAction
                    {
                        Name        = "SaveInvoice",
                        Description = "Save extracted invoice data. Only call after explicit user confirmation.",
                        ParametersSampleObject = JsonConvert.SerializeObject(new VeritySaveInvoiceArgs
                        {
                            InvoiceNumber = "INV-001",
                            VendorName    = "Vendor name",
                            InvoiceDate   = "yyyy-MM-dd",
                            DueDate       = "yyyy-MM-dd",
                            TotalAmount   = 0.00m,
                            VatAmount     = 0.00m,
                            Currency      = "PLN",
                            Description   = "Brief description of the invoice",
                            LineItems     =
                            [
                                new VerityInvoiceLineItem
                                {
                                    Description = "Item description",
                                    Quantity    = 1,
                                    UnitPrice   = 0.00m,
                                }
                            ]
                        })
                    },
                    new AiAgentToolAction
                    {
                        Name        = "FlagInvoice",
                        Description = "Flag an invoice for review or mark as duplicate/suspicious",
                        ParametersSampleObject = JsonConvert.SerializeObject(new VerityFlagInvoiceArgs
                        {
                            InvoiceId = "invoices/1-A",
                            Reason    = "Duplicate | Suspicious | MissingData | Other",
                            Note      = "Additional context about the flag"
                        })
                    }
                ]
            });
    }
}