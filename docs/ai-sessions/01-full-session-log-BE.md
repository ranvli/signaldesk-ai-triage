# SignalDesk — Full AI-Assisted Build Session Log

**Tool:** GitHub Copilot Chat (Visual Studio 2026 Insiders)  
**Date:** July 2025  
**Repo:** https://github.com/ranvli/signaldesk-ai-triage  
**Branch:** main

---

## What I was trying to do

Build a complete AI-assisted feedback triage SaaS tool (SignalDesk) from scratch as a take-home submission. The challenge required:

- .NET 8 minimal API backend
- Vue 3 frontend
- SQLite persistence
- Local Ollama LLM for classification
- At least one real test
- Documentation: clarifying questions, architecture, test strategy, risks, design pack, AI session logs

I used GitHub Copilot as a coding and architecture partner throughout. This log documents the full session — what I prompted, what the AI produced, what I corrected, and what decisions I made.

---

## Phase 1 — Scaffold: Solution Structure

### What I prompted
Asked Copilot to create a 4-project .NET 8 solution for the SignalDesk MVP from a boilerplate that was already targeting net10.0.

### What the AI produced
- Identified the existing `SignalDesk.csproj` targeting `net10.0` with a WeatherForecast controller
- Repurposed it as the API project, changed TFM to `net8.0`
- Created `SignalDesk.Domain`, `SignalDesk.Infrastructure`, `SignalDesk.Tests` as new projects
- Updated `SignalDesk.slnx` to include all four projects

### Errors encountered and fixed
- **Package version conflict:** `Microsoft.Extensions.Options` was at 8.0.0 but `Microsoft.Extensions.Http` required 8.0.2. Bumped to 8.0.2.
- **Raw string literal bug:** `$$"""` caused CS9006. Fixed by using standard interpolation for JSON braces.
- **Missing using:** `System.Net.Http.Json` was missing in the AI service file. Added manually.
- **AddOpenApi() / MapOpenApi():** These are .NET 9-only APIs. Replaced with `Swashbuckle.AspNetCore 6.9.0`.

### Decision I made
Kept the existing project file rather than deleting and recreating. Faster and avoids GUID conflicts in the slnx.

---

## Phase 2 — Domain Layer

### What I prompted
Asked Copilot to generate the domain model with specific enum values and entity fields matching the challenge spec.

### What the AI produced
- `FeedbackItem`: `Guid Id`, `string Text`, `string Summary`, `FeedbackCategory Category`, `FeedbackStatus Status`, `FeedbackPriority Priority`, `DateTime CreatedAt`
- `FeedbackCategory`: `Bug, FeatureRequest, Complaint, Praise`
- `FeedbackStatus`: `Open, Actioned, Dismissed`
- `FeedbackPriority`: `Low, Medium, High`
- Renamed `Priority.cs` → `FeedbackPriority.cs` (I left the old empty `Priority.cs` file — noted as cleanup debt)

### Correction I made
The AI initially included a `Performance` and `UX` value in `FeedbackCategory` that I did not ask for. I rejected those and kept only the four required values.

---

## Phase 3 — Infrastructure: Data Layer

### What I prompted
Asked for EF Core 8 + SQLite setup with migrations-ready configuration.

### What the AI produced
- `SignalDeskDbContext` using `ApplyConfigurationsFromAssembly`
- `FeedbackItemConfiguration : IEntityTypeConfiguration<FeedbackItem>` — enums stored as strings, required fields
- `SignalDeskDbContextFactory : IDesignTimeDbContextFactory<SignalDeskDbContext>` — enables `dotnet ef migrations add` without specifying startup project

### Decision I made
Used `EnsureCreated()` at startup instead of migrations for MVP. The factory is there so migrations can be added later without touching `Program.cs`.

---

## Phase 4 — Infrastructure: AI Service

### What I prompted
Asked for an Ollama integration behind an `IFeedbackAiService` interface so the AI provider is swappable.

### What the AI produced
- `IFeedbackAiService` with `Task<AiAnalysisResult> AnalyzeAsync(string text)`
- `AiAnalysisResult` record: `(Summary, Category, Priority)`
- `OllamaFeedbackAiService` with:
  - `SystemPrompt` const with full role definition, closed-vocabulary rules, exact JSON schema
  - `_categoryMap` dictionary: maps snake_case model output (`feature_request`) to PascalCase enum names (`FeatureRequest`)
  - `StripCodeFences()`: handles models that wrap JSON in ```json blocks despite instructions
  - `Fallback()`: deterministic fallback to `Complaint / Medium / truncated text` on any failure
  - Private HTTP DTOs: `OllamaRequest`, `OllamaResponse`, `OllamaAiResult`

### Error I caught and corrected
After the initial domain update, there was a stale reference to `FeedbackCategory.Other` in the AI service (the old enum had an `Other` value). I caught the build error and replaced it with `FeedbackCategory.Complaint`.

### Decision I made
Separated `system` and `prompt` fields in the Ollama request. The system prompt is static across all requests. The user prompt contains only the raw feedback text. This mirrors Ollama's intended API usage and produces more consistent output than embedding instructions in the user prompt.

---

## Phase 5 — Infrastructure: Repository

### What I prompted
Asked for a thin repository layer so endpoints never touch `DbContext` directly.

### What the AI produced
- `IFeedbackRepository`: `Add`, `GetAllAsync`, `GetByIdAsync(Guid)`, `SaveChangesAsync`, `GetCategoryCountsAsync`
- `FeedbackRepository`: DB-level `GROUP BY` for category counts, in-memory zero-fill for categories with no items

### Decision I made
Kept `GetCategoryCountsAsync` on the interface even though `GetStatsAsync` in the endpoint ended up doing the aggregation in-memory via LINQ. The DB-level version exists but is currently unused by the stats endpoint — acknowledged as cleanup debt.

---

## Phase 6 — API Layer

### What I prompted
Asked for minimal API endpoints with CORS, Swagger, and full DI wiring.

### What the AI produced
- `CreateFeedbackRequest(string Text)` — single field, no priority param (AI sets it)
- `FeedbackResponse(Guid Id, Text, Summary, Category, Status, Priority, DateTime CreatedAt)` — all strings for enum fields
- `StatsResponse(Dictionary<string,int> ByCategory, Dictionary<string,int> ByPriority)`
- `FeedbackEndpoints` with 5 routes:
  - `POST /feedback`
  - `GET /feedback`
  - `PATCH /feedback/{id:guid}/action`
  - `PATCH /feedback/{id:guid}/dismiss`
  - `GET /stats`
- `Program.cs`: full DI, CORS `WithOrigins("http://localhost:5173")`, Swagger under `IsDevelopment()`

### Correction I made
The AI initially registered CORS with `AllowAnyOrigin()`. I rejected that and specified the exact Vite dev server origin `http://localhost:5173` to avoid establishing a permissive baseline.

---

## Phase 7 — Tests

### What I prompted
Asked for meaningful tests, not trivial smoke tests.

### What the AI produced

**Integration tests (`FeedbackEndpointsTests.cs`) — 6 tests:**
- `PostFeedback_ReturnsCreated_WithAiFields`
- `GetFeedback_ReturnsItems`
- `PatchAction_SetsStatusToActioned`
- `PatchDismiss_SetsStatusToDismissed`
- `PatchAction_UnknownId_ReturnsNotFound`
- `GetStats_ReturnsCategoryAndPriorityCounts`

Uses `WebApplicationFactory<Program>` with two DI substitutions:
- Real SQLite `:memory:` (not EF in-memory provider — preserves GROUP BY semantics)
- `FakeFeedbackAiService` returning deterministic `Bug / Medium`

**Unit tests (`OllamaFeedbackAiServiceTests.cs`) — 8 tests:**
- `UnrecognisedCategory_FallsBackToComplaint`
- `UnrecognisedCategory_StillParsesPriority`
- `MalformedJson_FallsBackToComplaint`
- `MalformedJson_FallsBackToMediumPriority`
- `MalformedJson_SummaryIsTruncatedInput`
- `MalformedJson_LongInput_SummaryIsTruncatedTo120`
- `OllamaReturnsHttpError_FallsBackToDefaults`
- `FeatureRequestSnakeCase_MapsToEnum`

Uses `StubHttpMessageHandler` — no Moq, no NSubstitute, no live Ollama.

**Total: 14/14 passing.**

### Why the `feature_request` test matters
`Enum.TryParse` silently returns `false` for snake_case values. The `_categoryMap` dictionary is the only thing preventing permanent miscategorisation of feature requests. This test would catch a regression if the map were removed.

---

## Phase 8 — Critical Bug: Ollama Always Falling Back

### Symptom observed at runtime
Every `POST /feedback` inserted successfully but with `Category=Complaint`, `Priority=Medium`. The logs showed `System.InvalidOperationException` in `System.Net.Http.dll` before each insert.

### What I prompted
Asked Copilot to audit the Ollama HTTP integration and find the exact cause.

### Root cause identified
`Program.cs` had two separate DI registrations:

```csharp
// This configures the typed client for OllamaFeedbackAiService directly
builder.Services.AddHttpClient<OllamaFeedbackAiService>(...);

// This registers the interface separately — creates a NEW DI entry
// ASP.NET resolves IFeedbackAiService via this registration,
// which constructs OllamaFeedbackAiService with a plain default HttpClient
// that has BaseAddress = null
builder.Services.AddScoped<IFeedbackAiService, OllamaFeedbackAiService>();
```

When the endpoint resolves `IFeedbackAiService`, the scoped registration constructs `OllamaFeedbackAiService` directly via DI — bypassing the `AddHttpClient` factory. The injected `HttpClient` has no `BaseAddress`. The call to `PostAsJsonAsync("/api/generate", ...)` with a relative URI on a client with no base throws `InvalidOperationException`. The `catch` block returns `Fallback()` silently every time.

### Fix applied
Replaced two registrations with one:

```csharp
// BEFORE (broken)
builder.Services.AddHttpClient<OllamaFeedbackAiService>((sp, client) => { ... });
builder.Services.AddScoped<IFeedbackAiService, OllamaFeedbackAiService>();

// AFTER (fixed)
builder.Services.AddHttpClient<IFeedbackAiService, OllamaFeedbackAiService>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl + "/");  // trailing slash required
    client.Timeout = TimeSpan.FromSeconds(60);
});
```

Also added trailing slash to `BaseUrl`. `HttpClient` requires the base address to end with `/` for RFC 3986 URI combination to work correctly. Without it, a base like `http://host/v1` combined with `api/generate` silently drops `/v1`.

### What this taught me
`AddHttpClient<TImplementation>` and `AddScoped<TInterface, TImplementation>` are not equivalent to `AddHttpClient<TInterface, TImplementation>`. The first pair creates two independent DI entries. Only the second form correctly wires the configured typed client to the interface resolution path.

---

## Phase 9 — Frontend Integration Audit

### What I prompted
Asked for an exact API contract audit — what the frontend must call vs. what the backend actually exposes.

### What the audit found (all fatal mismatches)

| # | Problem | Frontend code | Backend reality |
|---|---------|---------------|-----------------|
| 1 | Wrong base URL | `http://localhost:5000` | `http://localhost:5180` |
| 2 | Wrong path prefix | `/api/feedback` | `/feedback` (no /api) |
| 3 | Wrong action routes | `PATCH /api/feedback/{id}/status` with body | `PATCH /feedback/{id}/action` and `/dismiss`, no body |
| 4 | Wrong stats route | `/api/feedback/stats` | `/stats` |
| 5 | Wrong CORS origin | Vite on port `61814` | CORS allows only `5173` |
| 6 | Wrong stats keys | `stats.categories`, `stats.priorities` | `byCategory`, `byPriority` |
| 7 | Missing field | `stats.statuses`, `stats.total` | Not returned by backend |
| 8 | Wrong enum casing | `bug`, `feature_request` as lookup keys | `Bug`, `FeatureRequest` from API |

The frontend as written could not successfully call any backend endpoint. The audit made all of this explicit before touching any code.

---

## Phase 10 — Launch Configuration Fix

### What I prompted
Asked to make Swagger open automatically when starting from Visual Studio.

### What was wrong
Both profiles in `launchSettings.json` had `"launchBrowser": false` and no `"launchUrl"` key.

### Fix applied
Added to both `http` and `https` profiles:
```json
"launchBrowser": true,
"launchUrl": "swagger"
```

`Program.cs` already had `UseSwagger()` and `UseSwaggerUI()` correctly gated behind `IsDevelopment()`. No change needed there.

---

## Phase 11 — Documentation

All documentation was written with Copilot based on the actual code — not invented in advance.

### docs/clarifying-questions.md
10 questions covering: sync vs async classification, queue vs audit log semantics, human override policy, Ollama unavailability strategy, source field, stats time-range filtering, text length limits, delete vs append-only, assignment/auth, and reclassification on model change. Each includes rationale and assumed answer.

### docs/architecture.md
Covers: 4-project structure, dependency direction, data model, API table, POST /feedback sequence diagram, AI integration design (system/prompt split, `_categoryMap`, fallback contract), hallucination mitigation table, auth approach, and 6 key architectural decisions.

### docs/test-strategy.md
Distinguishes between implemented tests (14), recommended next tests (contract/schema tests, golden-set eval), and future production strategy (model drift detection). Does not claim tests that do not exist.

### docs/risks.md
5 risks grounded in actual implementation:
1. Sync Ollama blocks POST — 60s timeout, no Processing state
2. Immutable classification — no override endpoint exists
3. No ModelVersion on FeedbackItem — silent drift undetectable
4. Unbounded text input — no MaxLength on CreateFeedbackRequest
5. Forced single-label — compound feedback signals permanently discarded

### docs/discovery-design-pack.md
The challenge template was already in the repo as a blank form. Kept that as-is. Separate `discovery-design.md` contains the filled-out version.

---

## Key Decisions Summary

| Decision | What I chose | Why |
|----------|-------------|-----|
| DI registration for typed HttpClient | `AddHttpClient<IInterface, TImpl>` | Only form that correctly binds configured client to interface resolution |
| AI fallback strategy | Always save, never fail | Availability over correctness for internal MVP |
| Enum serialisation | String names via `JsonStringEnumConverter` | Human-readable DB, safe against reorder bugs |
| Stats aggregation | In-memory LINQ after `GetAllAsync` | Simpler for MVP; acceptable at small data volume |
| CORS | Pinned to `localhost:5173` | Rejected `AllowAnyOrigin` to avoid permissive baseline |
| Migrations | `EnsureCreated` at startup | `IDesignTimeDbContextFactory` present so migrations can be added later |
| Test isolation | Real SQLite `:memory:` + `FakeFeedbackAiService` | EF query semantics preserved; AI non-determinism removed |
| Auth | None for MVP | Single-operator localhost tool; auth is orthogonal to core value |

---

## What I would do differently in production

- Make classification async (background queue, polling endpoint)
- Add `ModelVersion` field to `FeedbackItem`
- Add `PATCH /feedback/{id}/classification` for human overrides
- Add `[MaxLength(2000)]` on `CreateFeedbackRequest.Text`
- Add golden-set evaluation harness for model upgrades
- Pin Vite port to `5173` to match the CORS config, or make CORS origin configurable
