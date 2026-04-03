using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.AI;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Operations.AI;
using Raven.Client.Documents.Operations.AI.Agents;
using Raven.Client.Documents.Operations.ConnectionStrings;
using Raven.Client.Documents.Session;
using RavenDB.Samples.Verity.App.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace RavenDB.Samples.Verity.App;

// ── Azure Functions API ───────────────────────────────────────
public class Api(
    ILogger<Api> logger,
    IAsyncDocumentSession session,
    IDocumentStore store,
    IConfiguration config,
    SecEdgarApi edgar)
{

    // GET /api/test
    [Function(nameof(test))]
    public async Task<IActionResult> test(
        [HttpTrigger("get", Route = "test")] HttpRequest req)
    {
        return new JsonResult("Welcome to Azure Functions!");
    }

    // POST /api/read-pdf
    [Function(nameof(ReadPdf))]
    public async Task<IActionResult> ReadPdf(
        [HttpTrigger("post", Route = "read-pdf")] HttpRequest req)
    {
        if (!req.HasFormContentType || req.Form.Files.Count == 0)
            return new BadRequestObjectResult("Wyślij plik PDF jako form-data.");

        var file = req.Form.Files[0];

        if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return new BadRequestObjectResult("Plik musi być w formacie PDF.");

        using var stream = file.OpenReadStream();
        using var pdf = PdfDocument.Open(stream);

        var pages = pdf.GetPages().Select(p => new
        {
            page = p.Number,
            text = p.Text,
            words = p.GetWords().Select(w => w.Text).ToList()
        }).ToList(); // <-- materializuje przed disposed

        return new JsonResult(new
        {
            fileName = file.FileName,
            totalPages = pdf.NumberOfPages,
            pages
        });
    }

    // GET /api/reports
    [Function(nameof(GetReports))]
    public async Task<IActionResult> GetReports(
        [HttpTrigger("get", Route = "reports")] HttpRequest req)
    {
        var reports = await session.Query<Report>()
                                    .OrderByDescending(x => x.AccessionNumber)
                                    .ToListAsync();

        return new JsonResult(reports);
    }

    // POST /api/fetch-10q?cik=320193&max=5
    [Function(nameof(Fetch10Q))]
    public async Task<IActionResult> Fetch10Q(
        [HttpTrigger("post", Route = "fetch-10q")] HttpRequest req)
    {
        var cik = req.Query["cik"].ToString();
        if (string.IsNullOrWhiteSpace(cik))
            return new BadRequestObjectResult("Podaj parametr 'cik' (np. ?cik=320193).");

        if (!int.TryParse(req.Query["max"], out var max) || max < 1)
            max = 5;

        await edgar.FetchAndSaveAll10QsAsync(cik, max, req.HttpContext.RequestAborted);

        return new OkObjectResult(new { cik, max, status = "Zapisano raporty 10-Q w RavenDB." });
    }
}