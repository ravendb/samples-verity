using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using RavenDB.Samples.Verity.Model;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RavenDB.Samples.Verity.App.Infrastructure;

public class VerityAgentApi(
    ILogger<VerityAgentApi> logger,
    IAsyncDocumentSession session,
    IDocumentStore store,
    IHttpClientFactory httpClientFactory)
{
    // GET /api/agent/audit/context?reportId=Reports/1-A&userId=Users/Acme/John+Smith
    // Returns the structured context the frontend injects as the opening message to the agent.
    [Function(nameof(GetAuditAgentContext))]
    public async Task<IActionResult> GetAuditAgentContext(
        [HttpTrigger("get", Route = "agent/audit/context")] HttpRequest req)
    {
        var reportId = req.Query["reportId"].ToString().Trim();
        var userId   = req.Query["userId"].ToString().Trim();

        if (string.IsNullOrWhiteSpace(reportId) || string.IsNullOrWhiteSpace(userId))
            return new BadRequestObjectResult("Provide 'reportId' and 'userId' query parameters.");

        var report = await session.LoadAsync<Report>(reportId, req.HttpContext.RequestAborted);
        if (report is null)
            return new NotFoundObjectResult($"Report '{reportId}' not found.");

        var user = await session.LoadAsync<User>(userId, req.HttpContext.RequestAborted);
        if (user is null)
            return new NotFoundObjectResult($"User '{userId}' not found.");

        // Attempt to read the report HTML attachment for the agent to analyse
        var attachmentResult = await session.Advanced.Attachments.GetAsync(
            reportId, "form10-q.htm", req.HttpContext.RequestAborted)
            ?? await session.Advanced.Attachments.GetAsync(
                reportId, "form10-k.htm", req.HttpContext.RequestAborted);

        string reportText;
        if (attachmentResult is not null)
        {
            using var reader = new System.IO.StreamReader(attachmentResult.Stream);
            var html = await reader.ReadToEndAsync(req.HttpContext.RequestAborted);
            // Strip tags for a cleaner plain-text feed to the model
            reportText = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
            reportText = System.Text.RegularExpressions.Regex.Replace(reportText, @"\s{2,}", " ").Trim();
            if (reportText.Length > 40_000)
                reportText = reportText[..40_000] + "\n[truncated]";
        }
        else
        {
            reportText = report.Summary ?? "No report content available.";
        }

        var context = new AuditAgentContext
        {
            Auditor = new AuditAgentAuditor
            {
                UserId  = user.Id,
                Name    = user.Name,
                Surname = user.Surname,
                Email   = user.Email
            },
            Report = new AuditAgentReport
            {
                ReportId        = report.Id,
                FormType        = report.FormType,
                Year            = report.Year,
                Quarter         = report.Quarter,
                ReportDate      = report.ReportDate,
                AccessionNumber = report.AccessionNumber
            },
            ReportText = reportText
        };

        return new JsonResult(context);
    }

    // POST /api/agent/audit/generate
    // One-shot endpoint: given a reportId and userId it builds the context and asks the AI
    // to produce a draft audit text. Returns { notes: string } — no conversation.
    [Function(nameof(GenerateAuditNotes))]
    public async Task<IActionResult> GenerateAuditNotes(
        [HttpTrigger("post", Route = "agent/audit/generate")] HttpRequest req)
    {
        GenerateAuditRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<GenerateAuditRequest>(
                req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                req.HttpContext.RequestAborted);
        }
        catch
        {
            return new BadRequestObjectResult("Invalid JSON body.");
        }

        if (body is null || string.IsNullOrWhiteSpace(body.ReportId) || string.IsNullOrWhiteSpace(body.UserId))
            return new BadRequestObjectResult("Provide 'reportId' and 'userId'.");

        var report = await session.LoadAsync<Report>(body.ReportId, req.HttpContext.RequestAborted);
        if (report is null)
            return new NotFoundObjectResult($"Report '{body.ReportId}' not found.");

        var user = await session.LoadAsync<User>(body.UserId, req.HttpContext.RequestAborted);
        if (user is null)
            return new NotFoundObjectResult($"User '{body.UserId}' not found.");

        // Load HTML attachment, strip tags, truncate to 40k chars
        var attachmentResult = await session.Advanced.Attachments.GetAsync(
            body.ReportId, "form10-q.htm", req.HttpContext.RequestAborted)
            ?? await session.Advanced.Attachments.GetAsync(
                body.ReportId, "form10-k.htm", req.HttpContext.RequestAborted);

        string reportText;
        if (attachmentResult is not null)
        {
            using var reader = new System.IO.StreamReader(attachmentResult.Stream);
            var html = await reader.ReadToEndAsync(req.HttpContext.RequestAborted);
            reportText = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
            reportText = System.Text.RegularExpressions.Regex.Replace(reportText, @"\s{2,}", " ").Trim();
            if (reportText.Length > 40_000)
                reportText = reportText[..40_000] + "\n[truncated]";
        }
        else
        {
            reportText = report.Summary ?? "No report content available.";
        }

        var openAiKey = Environment.GetEnvironmentVariable(Constants.EnvVars.OpenAiApiKey);
        if (string.IsNullOrWhiteSpace(openAiKey))
            return new ObjectResult("OpenAI API key not configured.") { StatusCode = 503 };

        var systemPrompt =
            "You are a financial audit assistant. Given an SEC report filing and auditor details, " +
            "produce a concise professional audit text in the first person as the auditor. " +
            "Cover: revenue/expense trends, profitability assessment, asset position, key risks, and overall opinion. " +
            "Be factual and grounded only in the provided document. Output plain text only — no markdown.";

        var userMessage =
            $"Auditor: {user.Name} {user.Surname} ({user.Email})\n\n" +
            $"Report: {report.FormType} {report.Year} Q{report.Quarter} — {report.ReportDate}\n\n" +
            $"---\n{reportText}";

        var requestPayload = new
        {
            model    = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userMessage  }
            },
            max_completion_tokens = 1500,
            temperature           = 0.3
        };

        var httpClient = httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", openAiKey);

        var openAiResp = await httpClient.PostAsJsonAsync(
            "https://api.openai.com/v1/chat/completions",
            requestPayload,
            req.HttpContext.RequestAborted);

        if (!openAiResp.IsSuccessStatusCode)
        {
            var err = await openAiResp.Content.ReadAsStringAsync(req.HttpContext.RequestAborted);
            logger.LogError("OpenAI error {Status}: {Body}", openAiResp.StatusCode, err);
            return new ObjectResult($"OpenAI returned {(int)openAiResp.StatusCode}.") { StatusCode = 502 };
        }

        using var respStream = await openAiResp.Content.ReadAsStreamAsync(req.HttpContext.RequestAborted);
        using var doc = await JsonDocument.ParseAsync(respStream, cancellationToken: req.HttpContext.RequestAborted);
        var notes = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        logger.LogInformation("Generated audit notes for report {ReportId} by user {UserId}", body.ReportId, body.UserId);

        return new JsonResult(new { notes });
    }

    // POST /api/agent/audit/save
    // Action endpoint invoked by the RavenDB AI agent when it executes the SaveAudit action.
    [Function(nameof(AgentSaveAudit))]
    public async Task<IActionResult> AgentSaveAudit(
        [HttpTrigger("post", Route = "agent/audit/save")] HttpRequest req)
    {
        VeritySaveAuditArgs? args;
        try
        {
            args = await JsonSerializer.DeserializeAsync<VeritySaveAuditArgs>(
                req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                req.HttpContext.RequestAborted);
        }
        catch
        {
            return new BadRequestObjectResult("Invalid JSON body.");
        }

        if (args is null || string.IsNullOrWhiteSpace(args.ReportId))
            return new BadRequestObjectResult("Provide 'reportId' in the action args.");

        var report = await session.LoadAsync<Report>(args.ReportId, req.HttpContext.RequestAborted);
        if (report is null)
            return new NotFoundObjectResult($"Report '{args.ReportId}' not found.");

        var company = await session.LoadAsync<Company>(report.CompanyId, req.HttpContext.RequestAborted);
        if (company is null)
            return new NotFoundObjectResult($"Company '{report.CompanyId}' not found.");

        var auditId = $"Audits/{company.Name}/{report.Year}/Q{report.Quarter}";

        var audit = await session.LoadAsync<Audit>(auditId, req.HttpContext.RequestAborted);
        var isNew = audit is null;

        if (isNew)
        {
            audit = new Audit { Id = auditId, ReportId = report.Id };
            await session.StoreAsync(audit, req.HttpContext.RequestAborted);
        }

        audit!.AuditorName    = args.AuditorName;
        audit.AuditorSurname  = args.AuditorSurname;
        audit.AuditorEmail    = args.AuditorEmail;
        audit.AuditString     = args.AuditString;
        audit.GeneratedByAi   = true;

        await session.SaveChangesAsync(req.HttpContext.RequestAborted);

        logger.LogInformation("Agent saved audit {AuditId} (new={IsNew})", auditId, isNew);

        return new JsonResult(audit) { StatusCode = isNew ? StatusCodes.Status201Created : StatusCodes.Status200OK };
    }
}

// ── Request DTOs ─────────────────────────────────────────────
public record GenerateAuditRequest
{
    public string ReportId { get; init; } = "";
    public string UserId   { get; init; } = "";
}

// ── Context DTOs ─────────────────────────────────────────────
public record AuditAgentAuditor
{
    public string UserId  { get; init; } = "";
    public string Name    { get; init; } = "";
    public string Surname { get; init; } = "";
    public string Email   { get; init; } = "";
}

public record AuditAgentReport
{
    public string  ReportId        { get; init; } = "";
    public string  FormType        { get; init; } = "";
    public int?    Year            { get; init; }
    public int?    Quarter         { get; init; }
    public string  ReportDate      { get; init; } = "";
    public string  AccessionNumber { get; init; } = "";
}

public record AuditAgentContext
{
    public AuditAgentAuditor Auditor    { get; init; } = new();
    public AuditAgentReport Report { get; init; } = new();
    public string ReportText { get; init; } = "";
}