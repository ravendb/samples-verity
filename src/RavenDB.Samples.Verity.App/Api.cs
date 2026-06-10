using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Raven.Client.Documents;
using Raven.Client.Documents.AI;
using Raven.Client.Documents.Changes;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Operations.AI;
using Raven.Client.Documents.Operations.AI.Agents;
using Raven.Client.Documents.Operations.ConnectionStrings;
using Raven.Client.Documents.Operations.Revisions;
using Raven.Client.Documents.Session;
using System.Reactive.Linq;
using RavenDB.Samples.Verity.App.Infrastructure;
using RavenDB.Samples.Verity.Model;
using RavenDB.Samples.Verity.Setup;
using RavenDB.Samples.Verity.Model.Tasks;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RavenDB.Samples.Verity.App;

// ── Azure Functions API ───────────────────────────────────────
public class Api(
    IAsyncDocumentSession session,
    IDocumentStore store,
    SecEdgarApi edgar)
{

    private static string? GetSubjectFromBearer(HttpRequest req)
    {
        return req.HttpContext.User.FindFirst("sub")?.Value
              ?? DecodeJwtClaims(req).GetValueOrDefault("sub")?.FirstOrDefault();
    }
    private async Task<User?> GetCurrentUserAsync(HttpRequest req)
    {
        var sub = GetSubjectFromBearer(req);
        if (sub is null) return null;
        return await session.LoadAsync<User>(User.BuildId(sub), req.HttpContext.RequestAborted);
    }
    private static bool CanAccessCompany(User user, string companyId) =>
    user.Role == UserRole.Admin ||
    (user.Role == UserRole.Analyst && user.CompanyIds.Contains(companyId));

    // Decodes the JWT payload without signature validation.
    // The BFF has already validated the token; we only need the claims.
    private static Dictionary<string, string[]> DecodeJwtClaims(HttpRequest req)
    {
        var auth = req.Headers.Authorization.ToString();
        if (!auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return [];

        var parts = auth["Bearer ".Length..].Split('.');
        if (parts.Length < 2) return [];

        var padded = parts[1].Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateObject()
                .Where(p => p.Value.ValueKind is JsonValueKind.String or JsonValueKind.Array)
                .ToDictionary(
                    p => p.Name,
                    p => p.Value.ValueKind == JsonValueKind.Array
                        ? p.Value.EnumerateArray()
                                  .Where(e => e.ValueKind == JsonValueKind.String)
                                  .Select(e => e.GetString()!)
                                  .ToArray()
                        : [p.Value.GetString()!]);
        }
        catch { return []; }
    }

    // OPTIONS * — CORS preflight handler
    [Function(nameof(CorsPreflightHandler))]
    public IActionResult CorsPreflightHandler(
        [HttpTrigger("options", Route = "{**path}")] HttpRequest req)
    {
        return new OkResult();
    }

    // GET /api/reports?cik=320193
    [Authorize]
    [Function(nameof(GetReports))]
    public async Task<IActionResult> GetReports(
        [HttpTrigger("get", Route = "reports")] HttpRequest req)
    {
        var rawCik = req.Query["cik"].ToString();

        if (!string.IsNullOrWhiteSpace(rawCik))
        {
            var cik = SecEdgar.NormalizeCik(rawCik);

            var company = await session.Query<Company>()
                                       .FirstOrDefaultAsync(c => c.Cik == cik);

            if (company is null)
                return new NotFoundObjectResult($"Company with CIK {cik} not found.");

            var currentUser = await GetCurrentUserAsync(req);
            if (currentUser?.Role == UserRole.Analyst && !currentUser.CompanyIds.Contains(company.Id))
                return new ObjectResult("Access to this company is not allowed.") { StatusCode = 403 };

            var reports = await session.Query<Report>()
                                       .Where(r => r.CompanyId == company.Id)
                                       .OrderByDescending(r => r.ReportDate)
                                       .ToListAsync();

            return new JsonResult(reports);
        }

        return new NotFoundObjectResult($"No CIK number provided");
    }
    [Authorize(Roles = nameof(UserRole.Admin))]
    [Function(nameof(GetEvents))]
    public async Task<IActionResult> GetEvents([HttpTrigger("get", Route = "security/events")] HttpRequest req)
    {
        var page = int.TryParse(req.Query["page"], out var p) && p > 0 ? p : 1;
        var pageSize = int.TryParse(req.Query["pageSize"], out var ps) && ps > 0 ? Math.Min(ps, 50) : 20;
        var skip = (page - 1) * pageSize;

        var events = await session.Query<SecurityEvent>()
                                  .Statistics(out var stats)
                                  .OrderByDescending(e => e.At)
                                  .Skip(skip)
                                  .Take(pageSize)
                                  .ToListAsync();

        var totalPages = (int)Math.Ceiling(stats.TotalResults / (double)pageSize);
        return new JsonResult(new PagedResult<SecurityEvent>(events, page, pageSize, totalPages));
    }

    // GET /api/report?accession=0000320193-24-000123
    [Authorize]
    [Function(nameof(GetReport))]
    public async Task<IActionResult> GetReport(
        [HttpTrigger("get", Route = "report")] HttpRequest req)
    {
        var accession = req.Query["accession"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(accession))
            return new BadRequestObjectResult("Provide the 'accession' parameter.");

        var report = await session.Query<Report>()
                                  .FirstOrDefaultAsync(r => r.AccessionNumber == accession);

        if (report is null)
            return new NotFoundObjectResult($"Report with accession number '{accession}' not found.");

        var currentUser = await GetCurrentUserAsync(req);
        if (currentUser?.Role == UserRole.Analyst && !currentUser.CompanyIds.Contains(report.CompanyId))
            return new ObjectResult("Access to this company is not allowed.") { StatusCode = 403 };

        return new JsonResult(report);
    }

    // GET /api/companies?page=1&pageSize=20
    [Authorize]
    [Function(nameof(GetCompanies))]
    public async Task<IActionResult> GetCompanies(
        [HttpTrigger("get", Route = "companies")] HttpRequest req)
    {
        var currentUser = await GetCurrentUserAsync(req);
        if (currentUser?.Role == UserRole.Analyst)
        {
            var loaded = await session.LoadAsync<Company>(currentUser.CompanyIds, req.HttpContext.RequestAborted);
            var list = loaded.Values.OfType<Company>().OrderBy(c => c.Name).ToList();
            return new JsonResult(new PagedResult<Company>(list, 1, list.Count, 1));
        }

        var page = int.TryParse(req.Query["page"], out var p) && p > 0 ? p : 1;
        var pageSize = int.TryParse(req.Query["pageSize"], out var ps) && ps > 0 ? Math.Min(ps, 100) : 20;
        var skip = (page - 1) * pageSize;

        var companies = await session.Query<Company>()
                                     .Statistics(out var stats)
                                     .OrderByDescending(c => c.Sic)
                                     .ThenBy(c => c.Name)
                                     .Skip(skip)
                                     .Take(pageSize)
                                     .ToListAsync();

        var total = stats.TotalResults;
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return new JsonResult(new PagedResult<Company>(companies, page, pageSize, totalPages));
    }

    // GET /api/users?companyId=Companies/...
    [Authorize(Roles = nameof(UserRole.Admin))]
    [Function(nameof(GetUsers))]
    public async Task<IActionResult> GetUsers(
        [HttpTrigger("get", Route = "users")] HttpRequest req)
    {
        var companyId = req.Query["companyId"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(companyId))
            return new BadRequestObjectResult("Provide the 'companyId' query parameter.");

        var users = await session.Query<User>()
                                 .Where(u => u.CompanyIds.Contains(companyId))
                                 .OrderBy(u => u.Surname)
                                 .ThenBy(u => u.Name)
                                 .ToListAsync();

        return new JsonResult(users);
    }

    // GET /api/users/me — returns the authenticated user's profile, creating it on first login.
    [Authorize]
    [Function(nameof(GetMe))]
    public async Task<IActionResult> GetMe(
        [HttpTrigger("get", Route = "users/me")] HttpRequest req)
    {
        var sub = GetSubjectFromBearer(req);
        if (sub is null)
            return new UnauthorizedResult();

        var id = User.BuildId(sub);
        var user = await session.LoadAsync<User>(id, req.HttpContext.RequestAborted);

        if (user is null)
        {
            var claims = DecodeJwtClaims(req);
            var fullName = claims.GetValueOrDefault("name", []).FirstOrDefault() ?? "";
            var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var roleString = claims.GetValueOrDefault("role", []).FirstOrDefault() ?? "Viewer";

            user = new User
            {
                Id = id,
                SubjectId = sub,
                Name = parts.ElementAtOrDefault(0) ?? fullName,
                Surname = parts.ElementAtOrDefault(1) ?? "",
                Email = claims.GetValueOrDefault("email", []).FirstOrDefault() ?? "",
                Role = Enum.TryParse<UserRole>(roleString, out var r) ? r : UserRole.Viewer,
                CompanyIds = [.. claims.GetValueOrDefault("company_id", [])],
            };

            await session.StoreAsync(user, id, req.HttpContext.RequestAborted);
            await session.SaveChangesAsync(req.HttpContext.RequestAborted);
        }

        return new JsonResult(user);
    }

    // PUT /api/users/me — update display name fields.
    [Authorize]
    [Function(nameof(UpdateMe))]
    public async Task<IActionResult> UpdateMe(
        [HttpTrigger("put", Route = "users/me")] HttpRequest req)
    {
        var sub = GetSubjectFromBearer(req);
        if (sub is null)
            return new UnauthorizedResult();

        var dto = await req.ReadFromJsonAsync<UpdateUserRequest>(req.HttpContext.RequestAborted);
        if (dto is null)
            return new BadRequestObjectResult("Invalid request body.");

        var id = User.BuildId(sub);
        var user = await session.LoadAsync<User>(id, req.HttpContext.RequestAborted);
        if (user is null)
            return new NotFoundObjectResult("User profile not found. Call GET /api/users/me first.");

        if (!string.IsNullOrWhiteSpace(dto.Name)) user.Name = dto.Name.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Surname)) user.Surname = dto.Surname.Trim();

        await session.SaveChangesAsync(req.HttpContext.RequestAborted);
        return new JsonResult(user);
    }

    // GET /api/company?cik=320193
    [Authorize]
    [Function(nameof(GetCompany))]
    public async Task<IActionResult> GetCompany(
        [HttpTrigger("get", Route = "company")] HttpRequest req)
    {
        var cik = SecEdgar.NormalizeCik(req.Query["cik"].ToString());
        if (string.IsNullOrWhiteSpace(cik))
            return new BadRequestObjectResult("Provide the 'cik' parameter (e.g., ?cik=320193).");

        var company = await session.Query<Company>()
                                   .FirstOrDefaultAsync(c => c.Cik == cik);

        if (company is null)
            return new NotFoundObjectResult($"Company with CIK {cik} does not exist in the database. Use POST /api/company to fetch it.");

        var currentUser = await GetCurrentUserAsync(req);
        if (currentUser?.Role == UserRole.Analyst && !currentUser.CompanyIds.Contains(company.Id))
            return new ObjectResult("Access to this company is not allowed.") { StatusCode = 403 };

        return new JsonResult(company);
    }

    // POST /api/company?cik=320193
    [Authorize(Roles = nameof(UserRole.Admin))]
    [Function(nameof(SaveCompany))]
    public async Task<IActionResult> SaveCompany(
        [HttpTrigger("post", Route = "company")] HttpRequest req)
    {
        var cik = req.Query["cik"].ToString();
        if (string.IsNullOrWhiteSpace(cik))
            return new BadRequestObjectResult("Provide the 'cik' parameter (e.g., ?cik=320193).");

        var paddedCik = SecEdgar.NormalizeCik(cik);
        var existing = await session.Query<Company>().FirstOrDefaultAsync(c => c.Cik == paddedCik, req.HttpContext.RequestAborted);
        if (existing is not null)
            return new ConflictObjectResult($"Company with CIK {paddedCik} already exists.");

        var company = await edgar.FetchAndSaveCompanyAsync(paddedCik, req.HttpContext.RequestAborted);
        return new JsonResult(company) { StatusCode = StatusCodes.Status201Created };
    }

    // POST /api/fetch-10q?cik=320193&max=5
    [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Analyst))]
    [Function(nameof(Fetch10Q))]
    public async Task<IActionResult> Fetch10Q(
        [HttpTrigger("post", Route = "fetch-10q")] HttpRequest req)
    {
        var cik = req.Query["cik"].ToString();
        if (string.IsNullOrWhiteSpace(cik))
            return new BadRequestObjectResult("Provide the 'cik' parameter (e.g., ?cik=320193).");

        if (!int.TryParse(req.Query["max"], out var max) || max < 1)
            max = 5;

        var paddedCik = SecEdgar.NormalizeCik(cik);

        // First try to find an existing company
        var company = await session.Query<Company>().FirstOrDefaultAsync(c => c.Cik == paddedCik);

        var currentUser = await GetCurrentUserAsync(req);
        if (currentUser is null) return new UnauthorizedResult();

        if (company is null)
        {
            // Only Admin can add a new company via this endpoint
            if (currentUser.Role != UserRole.Admin)
                return new ObjectResult("Company not found. Only Admin can add new companies.") { StatusCode = 403 };
            company = await edgar.FetchAndSaveCompanyAsync(paddedCik, req.HttpContext.RequestAborted);
        }
        else if (!CanAccessCompany(currentUser, company.Id))
        {
            return new ObjectResult("Access to this company is not allowed.") { StatusCode = 403 };
        }

        await edgar.FetchAndSaveAllFilingsAsync(company, max, req.HttpContext.RequestAborted);

        return new OkObjectResult(new { company.Cik, companyName = company.Name, max, status = "Saved 10-Q reports in RavenDB." });
    }

    // POST /api/audit  — creates an audit for a given report
    // Body (JSON): { reportId, auditorName, auditorSurname, auditorEmail, auditString }
    [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Analyst))]
    [Function(nameof(CreateAudit))]
    public async Task<IActionResult> CreateAudit(
        [HttpTrigger("post", Route = "audit")] HttpRequest req)
    {
        CreateAuditRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<CreateAuditRequest>(
                req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                req.HttpContext.RequestAborted);
        }
        catch
        {
            return new BadRequestObjectResult("Invalid JSON body.");
        }

        if (body is null || string.IsNullOrWhiteSpace(body.ReportId))
            return new BadRequestObjectResult("Provide 'reportId' in the request body.");

        // Load the report
        var report = await session.LoadAsync<Report>(body.ReportId, req.HttpContext.RequestAborted);
        if (report is null)
            return new NotFoundObjectResult($"Report '{body.ReportId}' not found.");

        // Load the company associated with the report
        var company = await session.LoadAsync<Company>(report.CompanyId, req.HttpContext.RequestAborted);
        if (company is null)
            return new NotFoundObjectResult($"Company '{report.CompanyId}' not found.");
        var currentUser = await GetCurrentUserAsync(req);
        if (currentUser is null) return new UnauthorizedResult();
        if (!CanAccessCompany(currentUser, company.Id))
            return new ObjectResult("Access to this company is not allowed.") { StatusCode = 403 };
        var auditId = Audit.BuildId(company, report);

        // Upsert: update existing audit or create a new one
        var audit = await session.LoadAsync<Audit>(auditId, req.HttpContext.RequestAborted);
        var isNew = audit is null;

        if (isNew)
        {
            audit = new Audit { Id = auditId, ReportId = report.Id };
            await session.StoreAsync(audit, req.HttpContext.RequestAborted);
        }

        audit!.AuditorName = body.AuditorName ?? string.Empty;
        audit.AuditorSurname = body.AuditorSurname ?? string.Empty;
        audit.AuditorEmail = body.AuditorEmail ?? string.Empty;
        audit.AuditString = body.AuditString ?? string.Empty;
        audit.GeneratedByAi = body.GeneratedByAi;

        await session.SaveChangesAsync(req.HttpContext.RequestAborted);

        return new JsonResult(audit) { StatusCode = isNew ? StatusCodes.Status201Created : StatusCodes.Status200OK };
    }

    // POST /api/audit/restore  — restores an audit document to a specific revision
    // Body (JSON): { auditId, changeVector }
    [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Analyst))]
    [Function(nameof(RestoreAuditRevision))]
    public async Task<IActionResult> RestoreAuditRevision(
        [HttpTrigger("post", Route = "audit/restore")] HttpRequest req)
    {
        RestoreAuditRevisionRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<RestoreAuditRevisionRequest>(
                req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                req.HttpContext.RequestAborted);
        }
        catch
        {
            return new BadRequestObjectResult("Invalid JSON body.");
        }

        if (body is null || string.IsNullOrWhiteSpace(body.AuditId) || string.IsNullOrWhiteSpace(body.ChangeVector))
            return new BadRequestObjectResult("Provide 'auditId' and 'changeVector' in the request body.");
        var currentUser = await GetCurrentUserAsync(req);
        if (currentUser is null) return new UnauthorizedResult();
        var audit = await session.LoadAsync<Audit>(body.AuditId, req.HttpContext.RequestAborted);
        if (audit is null) return new NotFoundObjectResult($"Audit '{body.AuditId}' not found.");
        var report = await session.LoadAsync<Report>(audit.ReportId, req.HttpContext.RequestAborted);
        if (report is null) return new NotFoundObjectResult($"Report '{audit.ReportId}' not found.");
        if (!CanAccessCompany(currentUser, report.CompanyId))
            return new ObjectResult("Access to this company is not allowed.") { StatusCode = 403 };
        await store.Operations.SendAsync(
            new RevertRevisionsByIdOperation(body.AuditId, body.ChangeVector),
            token: req.HttpContext.RequestAborted);
        return new OkResult();
    }

    // GET /api/audit/revisions?reportId=Reports/...
    // Note: revisions must be enabled for the Audits collection in RavenDB Studio.
    [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Analyst))]
    [Function(nameof(GetAuditRevisions))]
    public async Task<IActionResult> GetAuditRevisions(
        [HttpTrigger("get", Route = "audit/revisions")] HttpRequest req)
    {
        var reportId = req.Query["reportId"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(reportId))
            return new BadRequestObjectResult("Provide the 'reportId' query parameter.");

        var audit = await session.Query<Audit>()
                                 .FirstOrDefaultAsync(a => a.ReportId == reportId, req.HttpContext.RequestAborted);

        if (audit is null)
            return new NotFoundObjectResult($"Audit for report '{reportId}' not found.");
        var currentUser = await GetCurrentUserAsync(req);
        if (currentUser is null) return new UnauthorizedResult();
        var report = await session.LoadAsync<Report>(audit.ReportId, req.HttpContext.RequestAborted);
        if (report is null) return new NotFoundObjectResult($"Report '{audit.ReportId}' not found.");
        if (!CanAccessCompany(currentUser, report.CompanyId))
            return new ObjectResult("Access to this company is not allowed.") { StatusCode = 403 };
        // RavenDB includes the current version as the first revision — no need to fetch it separately
        var revisions = await session.Advanced.Revisions
                                     .GetForAsync<Audit>(audit.Id, 0, 50, req.HttpContext.RequestAborted);

        var revisionDtos = revisions.Select(rev =>
        {
            var meta = session.Advanced.GetMetadataFor(rev);
            var lastModified = meta.TryGetValue("@last-modified", out var lm) ? lm?.ToString() ?? "" : "";
            var changeVector = meta.TryGetValue("@change-vector", out var cv) ? cv?.ToString() ?? "" : "";
            return new AuditRevisionDto(rev, changeVector, lastModified);
        }).ToList();

        return new JsonResult(revisionDtos);
    }

    // GET /api/audit?reportId=Reports/...
    [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Analyst))]
    [Function(nameof(GetAudit))]
    public async Task<IActionResult> GetAudit(
        [HttpTrigger("get", Route = "audit")] HttpRequest req)
    {
        var reportId = req.Query["reportId"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(reportId))
            return new BadRequestObjectResult("Provide the 'reportId' query parameter.");

        var audit = await session.Query<Audit>()
                                 .FirstOrDefaultAsync(a => a.ReportId == reportId, req.HttpContext.RequestAborted);

        if (audit is null)
            return new NotFoundObjectResult($"Audit for report '{reportId}' not found.");
        var currentUser = await GetCurrentUserAsync(req);
        if (currentUser is null) return new UnauthorizedResult();
        var report = await session.LoadAsync<Report>(audit.ReportId, req.HttpContext.RequestAborted);
        if (report is null) return new NotFoundObjectResult($"Report '{audit.ReportId}' not found.");
        if (!CanAccessCompany(currentUser, report.CompanyId))
            return new ObjectResult("Access to this company is not allowed.") { StatusCode = 403 };
        return new JsonResult(audit);
    }

    // GET /api/manage/users
    [Authorize(Roles = nameof(UserRole.Admin))]
    [Function(nameof(GetAllUsers))]
    public async Task<IActionResult> GetAllUsers(
        [HttpTrigger("get", Route = "manage/users")] HttpRequest req)
    {
        var users = await session.Query<User>()
                                 .Where(u => u.SubjectId != null)
                                 .OrderBy(u => u.Surname)
                                 .ThenBy(u => u.Name)
                                 .ToListAsync();
        return new JsonResult(users);
    }

    // PUT /api/manage/users/{subjectId}/role
    [Authorize(Roles = nameof(UserRole.Admin))]
    [Function(nameof(SetUserRole))]
    public async Task<IActionResult> SetUserRole(
        [HttpTrigger("put", Route = "manage/users/{subjectId}/role")] HttpRequest req, string subjectId)
    {
        var body = await req.ReadFromJsonAsync<SetRoleRequest>(req.HttpContext.RequestAborted);
        if (body is null || !Enum.TryParse<UserRole>(body.Role, out var newRole))
            return new BadRequestObjectResult("Provide 'role': Viewer, Analyst, or Admin.");

        var user = await session.LoadAsync<User>(User.BuildId(subjectId), req.HttpContext.RequestAborted);
        if (user is null) return new NotFoundObjectResult($"User '{subjectId}' not found.");

        user.Role = newRole;
        await session.SaveChangesAsync(req.HttpContext.RequestAborted);
        return new JsonResult(user);
    }

    // PUT /api/manage/users/{subjectId}/companies
    [Authorize(Roles = nameof(UserRole.Admin))]
    [Function(nameof(SetUserCompanies))]
    public async Task<IActionResult> SetUserCompanies(
        [HttpTrigger("put", Route = "manage/users/{subjectId}/companies")] HttpRequest req, string subjectId)
    {
        var body = await req.ReadFromJsonAsync<SetCompaniesRequest>(req.HttpContext.RequestAborted);
        if (body is null)
            return new BadRequestObjectResult("Provide 'companyIds' array.");

        var user = await session.LoadAsync<User>(User.BuildId(subjectId), req.HttpContext.RequestAborted);
        if (user is null) return new NotFoundObjectResult($"User '{subjectId}' not found.");

        user.CompanyIds = body.CompanyIds ?? [];
        await session.SaveChangesAsync(req.HttpContext.RequestAborted);
        return new JsonResult(user);
    }

    // QueueTrigger: "auditRevisions" → save AuditNotification to RavenDB
    [Function(nameof(OnAuditRevision))]
    public async Task OnAuditRevision(
        [QueueTrigger(AuditRevisionQueueEtlTask.QueueName, Connection = Constants.EnvVars.AzureStorageConnectionString)] string messageBody)
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var envelope = JsonSerializer.Deserialize<CloudEventEnvelope<AuditRevisionMessage>>(messageBody, opts);
        var msg = envelope?.Data;

        if (msg is null) return;

        using var notifSession = store.OpenAsyncSession();
        var notification = new AuditNotification
        {
            AuditId = msg.AuditId,
            CompanyName = msg.CompanyName,
            ReportYear = msg.ReportYear,
            ReportQuarter = msg.ReportQuarter,
            At = DateTime.UtcNow
        };

        await notifSession.StoreAsync(notification);
        notifSession.Advanced.GetMetadataFor(notification)["@expires"] =
            DateTime.UtcNow.AddMinutes(5);
        await notifSession.SaveChangesAsync();
    }

    // GET /api/audit/stream — SSE: push new AuditNotifications to client
    [Authorize]
    [Function(nameof(StreamAuditEvents))]
    public async Task StreamAuditEvents(
        [HttpTrigger("get", Route = "audit/stream")] HttpRequest req)
    {
        var res = req.HttpContext.Response;
        res.StatusCode = 200;
        res.Headers["Content-Type"] = "text/event-stream";
        res.Headers["Cache-Control"] = "no-cache";
        res.Headers["X-Accel-Buffering"] = "no";

        var ct = req.HttpContext.RequestAborted;
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        using var subscription = store.Changes()
            .ForDocumentsInCollection<AuditNotification>()
            .Subscribe(change =>
            {
                if (change.Type != DocumentChangeTypes.Put || ct.IsCancellationRequested) return;
                _ = Task.Run(async () =>
                {
                    using var s = store.OpenAsyncSession();
                    var n = await s.LoadAsync<AuditNotification>(change.Id, ct);
                    if (n is null) return;
                    var json = JsonSerializer.Serialize(n, jsonOptions);
                    await res.WriteAsync($"data: {json}\n\n", ct);
                    await res.Body.FlushAsync(ct);
                }, ct);
            });

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(25));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                await res.WriteAsync(": keepalive\n\n", ct);
                await res.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    // GET /api/report/stream — SSE: push new AuditNotifications to client
    [Authorize]
    [Function(nameof(StreamReportEvents))]
    public async Task StreamReportEvents(
        [HttpTrigger("get", Route = "report/stream")] HttpRequest req)
    {
        var res = req.HttpContext.Response;
        res.StatusCode = 200;
        res.Headers["Content-Type"] = "text/event-stream";
        res.Headers["Cache-Control"] = "no-cache";
        res.Headers["X-Accel-Buffering"] = "no";

        var ct = req.HttpContext.RequestAborted;
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        using var subscription = store.Changes()
            .ForDocumentsInCollection<Report>()
            .Subscribe(change =>
            {
                if (change.Type != DocumentChangeTypes.Put || ct.IsCancellationRequested) return;
                _ = Task.Run(async () =>
                {
                    using var s = store.OpenAsyncSession();
                    var n = await s.LoadAsync<Report>(change.Id, ct);
                    if (n is null) return;
                    var json = JsonSerializer.Serialize(n.Id, jsonOptions);
                    await res.WriteAsync($"data: {json}\n\n", ct);
                    await res.Body.FlushAsync(ct);
                }, ct);
            });

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(25));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                await res.WriteAsync(": keepalive\n\n", ct);
                await res.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException) { }
    }
}

// ── DTOs ─────────────────────────────────────────────────────
public record CreateAuditRequest(
    string? ReportId,
    string? AuditorName,
    string? AuditorSurname,
    string? AuditorEmail,
    string? AuditString,
    bool GeneratedByAi = false);

public record PagedResult<T>(IList<T> Items, int Page, int PageSize, int TotalPages);

public record AuditRevisionDto(Audit Data, string ChangeVector, string LastModified);

public record RestoreAuditRevisionRequest(string? AuditId, string? ChangeVector);

public record AuditRevisionMessage(
    string AuditId,
    string CompanyName,
    int ReportYear,
    int ReportQuarter);

public record UpdateUserRequest(string? Name, string? Surname);

public record CloudEventEnvelope<T>(T? Data);

public record SetRoleRequest(string? Role);
public record SetCompaniesRequest(List<string>? CompanyIds);
