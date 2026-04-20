# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

**Verity** is a financial SEC filing analysis platform that showcases RavenDB 7.2's AI capabilities. It fetches 10-Q/10-K filings from SEC EDGAR, uses RavenDB's native AI agent + GenAI tasks for analysis, stores audit history with full revision tracking, and presents everything through a SvelteKit frontend.

## Running Locally

Prerequisites: .NET 10, Node.js 22, RavenDB license, OpenAI API key, Azure Storage credentials.

```bash
# Orchestrate everything via .NET Aspire (starts RavenDB, Azure emulator, backend, frontend)
cd src/RavenDB.Samples.Verity.AppHost
dotnet run
# Aspire dashboard: http://localhost:15000
```

After services start, trigger the "Migrate DB" command from the Aspire dashboard (or POST to `/api/migrate`) to initialize indexes, seed companies, and configure AI.

Store local secrets with:
```bash
dotnet user-secrets set --id 8a63def4-dd24-4ac6-9074-5602c497b6cc "KeyName" "value"
```

## Build & Check Commands

```bash
# Backend
cd src
dotnet build                   # Build all .NET projects

# Frontend
cd src/RavenDB.Samples.Verity.Frontend
npm install
npm run build                  # Production build
npm run check                  # TypeScript + Svelte type check
npm run check:watch            # Watch mode
```

## Architecture

### Projects

| Project | Role |
|---|---|
| `App` | Azure Functions v4 backend — all API endpoints in a single `Api.cs` |
| `AppHost` | .NET Aspire orchestration — wires up all services and injects env vars |
| `Frontend` | Svelte 5 (runes mode) + SvelteKit frontend |
| `Model` | Shared domain models + RavenDB AI agent/task definitions |
| `Setup` | RavenDB migration runner (migrations 001–003) |
| `ServiceDefaults` | Shared Aspire service configuration |
| `InAppSubHandler` | Azure Queue-based subscription handler for async processing |

### Data Flow

1. User triggers a fetch → backend calls SEC EDGAR API
2. HTML filings stored as RavenDB attachments (backed by Azure Storage)
3. `ChunkAnalysisTask` (GenAI) summarizes each HTML chunk; `ProfitabilityTask` extracts structured financials
4. `VerityAuditAgent` (RavenDB-native agent) receives auditor prompt, queries RavenDB, and produces audit reports via OpenAI
5. Audits stored with full revision history for diffing

### Backend API (`App/Api.cs`)

All 30+ HTTP endpoints live in one monolithic Azure Functions class. No repository layer — handlers directly use `IAsyncDocumentSession` and `IDocumentStore`. Key route groups: `/api/company`, `/api/report`, `/api/audit`, `/api/fetch-10q`, `/api/migrate`.

### RavenDB AI Integration

The AI setup is done in migration `003_ConfigureAi`:
- Creates an OpenAI connection string in RavenDB
- Registers `VerityAuditAgent` — a declaratively defined agent with RavenDB queries as its data sources and HTTP webhook actions (calls back into `/api/audit`)
- Registers `ChunkAnalysisTask` and `ProfitabilityTask` as GenAI tasks that run on document ingestion

Agent and task definitions live in `Model/` as C# classes and are upserted into RavenDB at migration time.

### Frontend (`Frontend/`)

- SvelteKit file-based routing under `src/routes/`
- API calls go through thin wrappers in `src/lib/api.ts` (`apiUrl()`, `callApi<T>()`)
- Domain services in `src/lib/services/` (`companies.ts`, `reports.ts`, `audit.ts`)
- Backend URL injected at build time via `_BASE_API_HTTP_` (set in `vite.config.ts` from Aspire env)

### Database Migrations (`Setup/`)

Numbered migrations run in order via `MigrationStartup` hosted service:
- `001_ConfigureDatabase` — indexes, attachment storage config, revision policies
- `002_ImportCompanies` — seeds initial company data
- `003_ConfigureAi` — AI connection string, agent, and GenAI tasks

Migrations share a `MigrationContext` that carries API keys and config.

### Rate Limiting

`SessionApiUsageLimiter` and `GlobalApiUsageLimiter` enforce per-session and global caps on AI calls. Tracked via RavenDB time-series. Throws typed exceptions caught by the API layer.
