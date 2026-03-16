# AI Session Log — SignalDesk Full Build

**Tool:** GitHub Copilot (Visual Studio 2026 Insiders)  
**Repo:** https://github.com/ranvli/signaldesk-ai-triage  
**Branch:** main  
**Session scope:** Complete MVP build from scratch — backend, frontend integration audit, bug fixes, documentation

---

## What I was trying to do

Build a complete AI-assisted feedback triage SaaS MVP called **SignalDesk** as a take-home submission.

Requirements:
- .NET 8 Minimal API backend
- Vue 3 frontend
- SQLite persistence
- Local Ollama model for AI classification
- Bonus feature: AI-suggested priority

The starting point was a boilerplate .NET project targeting net10.0 with a WeatherForecast controller. Everything needed to be rebuilt.

---

## Phase 1 — Solution Scaffold

### What I prompted
"Create a simple .NET 8 backend for an MVP product called SignalDesk — AI-assisted feedback triage."

### What the AI did
- Identified the existing boilerplate was targeting net10.0 — changed TFM to net8.0
- Created a 4-project solution structure:
  - `SignalDesk` (API)
  - `SignalDesk.Domain`
  - `SignalDesk.Infrastructure`
  - `SignalDesk.Tests`
- Updated `SignalDesk.slnx` to include all four projects
- Deleted the legacy `WeatherForecast` controller and related files

### Errors found and corrected
- `Microsoft.Extensions.Options` version conflict between `8.0.0` and `8.0.2` — bumped to `8.0.2`
- `AddOpenApi()` / `MapOpenApi()` are .NET 9-only APIs — AI initially used them, I caught this and replaced with Swashbuckle 6.9.0
- Raw string literal `$$"""` was used for JSON braces in the prompt — caused CS9006, fixed by removing the extra `$`

### What I accepted vs rejected
- Accepted: 4-project structure, dependency direction (Domain → Infrastructure → API)
- Rejected: the initial use of `AddOpenApi()` — wrong for .NET 8, would fail at runtime

---

## Phase 2 — Domain Layer

### What I prompted
"Generate the Domain project — FeedbackItem entity and enums with these exact values."

### What the AI produced
- `FeedbackItem`: `Guid Id`, `string Text`, `string Summary`, `FeedbackCategory Category`, `FeedbackStatus Status`, `FeedbackPriority Priority`, `DateTime CreatedAt`
- `FeedbackCategory`: `Bug, FeatureRequest, Complaint, Praise`
- `FeedbackStatus`: `Open, Actioned, Dismissed`
- `FeedbackPriority`: `Low, Medium, High`

### What I corrected
- Initial generation used `Priority` as the enum name — renamed to `FeedbackPriority` for consistency
- An empty `Priority.cs` file was left behind as a stale artifact (identified during later audit, marked for deletion)
- A reference to `FeedbackCategory.Other` appeared in a service class after the enum was updated — replaced with `Complaint`

---

## Phase 3 — Infrastructure Layer

### What I prompted
"Generate the Infrastructure data layer — EF Core with SQLite, IEntityTypeConfiguration, IDesignTimeDbContextFactory."

### What the AI produced
- `SignalDeskDbContext` using `ApplyConfigurationsFromAssembly`
- `FeedbackItemConfiguration : IEntityTypeConfiguration<FeedbackItem>` — enums stored as strings, required fields
- `SignalDeskDbContextFactory : IDesignTimeDbContextFactory<SignalDeskDbContext>` — enables `dotnet ef migrations add` without specifying a startup project

### Design decision I steered
I asked for enums stored as strings rather than integers. The AI initially generated `HasConversion<int>()`. I corrected this — string storage keeps the database human-readable and prevents silent corruption if enum members are reordered.

---

## Phase 4 — AI Service

### What I prompted
"Generate an AI service that calls a local Ollama instance. Interface: IFeedbackAiService returning AiAnalysisResult."

### What the AI produced
- `IFeedbackAiService` — `Task<AiAnalysisResult> AnalyzeAsync(string text)`
- `AiAnalysisResult` — `record(string Summary, FeedbackCategory Category, FeedbackPriority Priority)`
- `OllamaFeedbackAiService` — with system/prompt split, `_categoryMap` for snake_case mapping, `StripCodeFences`, `Fallback()`
- `OllamaOptions` — `BaseUrl`, `Model` bound from `appsettings.json`

### What I steered
- Asked for the system prompt to be separated from the user prompt (Ollama `system` field vs `prompt` field)
- Asked for a closed-vocabulary approach — model is given exact allowed values, not open-ended
- Asked for explicit classification heuristics in the system prompt (e.g. "if broken behaviour → bug")

### What I caught and fixed
- Missing `using System.Net.Http.Json` — caused a compile error
- The AI initially used `FeedbackCategory.Other` in the fallback after the enum was updated — replaced with `Complaint`

---

## Phase 5 — Repository Layer

### What I prompted
"Create IFeedbackRepository and FeedbackRepository with these 5 methods: Add, GetAllAsync, GetByIdAsync, SaveChangesAsync, GetCategoryCountsAsync."

### What the AI produced
- `FeedbackRepository` with DB-level `GROUP BY` via EF Core LINQ for category counts
- Zero-fill for all enum values so every category is always present in the response
- `GetAllAsync` with `OrderByDescending(f => f.CreatedAt)`

### Design decision
I asked for zero-fill explicitly. Without it, categories with no items would be absent from the stats response, breaking the frontend bar charts.

---

## Phase 6 — API Endpoints

### What I prompted
"Generate 5 minimal API endpoints. No /api prefix. Use IFeedbackRepository and IFeedbackAiService."

### What the AI produced
- `POST /feedback` — classify + persist, return 201
- `GET /feedback` — all items newest first
- `PATCH /feedback/{id:guid}/action` — set status to Actioned
- `PATCH /feedback/{id:guid}/dismiss` — set status to Dismissed
- `GET /stats` — category + priority counts
- `FeedbackItemExtensions.ToResponse()` — maps entity to DTO
- `Program.cs` — full DI wiring, CORS `WithOrigins("http://localhost:5173")`, Swagger, `EnsureCreated`

### What I corrected
- Initial generation used a generic `PATCH /feedback/{id}/status` with a body — I split this into two explicit routes (`/action`, `/dismiss`) with no body, which is cleaner and easier to route-match
- `CreateFeedbackRequest` initially included a `Priority` parameter — removed, priority is AI-assigned only

---

## Phase 7 — System Prompt Tuning

### What I prompted
"Update the Ollama system prompt with a full role definition, closed-vocabulary rules, classification heuristics, and exact JSON schema."

### What the AI produced
The `SystemPrompt` const in `OllamaFeedbackAiService`:
- Role: "customer feedback triage assistant for an internal SaaS tool"
- Closed categories: `bug | feature_request | complaint | praise`
- Closed priorities: `low | medium | high`
- Explicit rules: broken behaviour → bug, new capability → feature_request, etc.
- Exact JSON schema the model must return
- Instruction to not include markdown or explanation

### What I steered
- I asked for snake_case category values in the prompt (`feature_request` not `FeatureRequest`) because that is what Ollama models naturally produce
- The `_categoryMap` dictionary handles the snake_case → PascalCase conversion before `Enum.TryParse`

---

## Phase 8 — Unit Tests for AI Fallback

### What I prompted
"Create meaningful xUnit unit tests for OllamaFeedbackAiService — specifically the fallback and error handling paths."

### What the AI produced
`OllamaFeedbackAiServiceTests.cs` with 8 tests using `StubHttpMessageHandler`:

| Test | What it covers |
|------|----------------|
| `UnrecognisedCategory_FallsBackToComplaint` | Model returns unknown category |
| `UnrecognisedCategory_StillParsesPriority` | Valid priority alongside invalid category |
| `MalformedJson_FallsBackToComplaint` | Model returns prose instead of JSON |
| `MalformedJson_FallsBackToMediumPriority` | Same scenario, priority assertion |
| `MalformedJson_SummaryIsTruncatedInput` | Short input — full text used as summary |
| `MalformedJson_LongInput_SummaryIsTruncatedTo120` | 200-char input truncated to 120 + ellipsis |
| `OllamaReturnsHttpError_FallsBackToDefaults` | Ollama returns HTTP 500 |
| `FeatureRequestSnakeCase_MapsToEnum` | `feature_request` correctly maps to `FeatureRequest` |

### Why these tests matter
The `feature_request` snake_case test is the most important. `Enum.TryParse` silently fails on snake_case values. The `_categoryMap` is the only thing preventing permanent miscategorisation. This test would catch any regression if that dictionary were removed.

### Approach I chose
Used `StubHttpMessageHandler` instead of Moq or NSubstitute. No additional libraries needed — the handler intercepts `HttpClient` at the transport layer and returns a controlled `HttpResponseMessage`.

---

## Phase 9 — Integration Tests

### What I prompted
(Same session as unit tests)

### What the AI produced
`FeedbackEndpointsTests.cs` with 6 integration tests using `WebApplicationFactory<Program>`:

| Test | What it covers |
|------|----------------|
| `PostFeedback_ReturnsCreated_WithAiFields` | Happy path POST — 201, AI fields populated |
| `GetFeedback_ReturnsItems` | Items from POST visible in GET |
| `PatchAction_SetsStatusToActioned` | PATCH /action changes status |
| `PatchDismiss_SetsStatusToDismissed` | PATCH /dismiss changes status |
| `PatchAction_UnknownId_ReturnsNotFound` | PATCH on non-existent Guid returns 404 |
| `GetStats_ReturnsCategoryAndPriorityCounts` | Stats returns byCategory and byPriority |

`TestWebApplicationFactory` replaces:
- `SignalDeskDbContext` → real EF Core over shared in-memory SQLite connection
- `IFeedbackAiService` → `FakeFeedbackAiService` (deterministic: always returns `Bug / Medium`)

All 14 tests (6 integration + 8 unit) passed on first run after fixes.

---

## Phase 10 — Critical Bug: Ollama HTTP Client DI Misconfiguration

### Observed behaviour
Every `POST /feedback` succeeded but always persisted `Category=Complaint`, `Priority=Medium`. The logs showed `System.InvalidOperationException` in `System.Net.Http.dll` before each insert.

### What I prompted
"Audit the Ollama HTTP integration. Every submission falls back to Complaint/Medium. Find the exact cause."

### Root cause identified
`Program.cs` had two separate registrations:

```csharp
// This configures a typed client for the concrete class only
builder.Services.AddHttpClient<OllamaFeedbackAiService>(...);

// This registers the interface via a separate DI entry
// ASP.NET Core resolves this independently — injects a default HttpClient with no BaseAddress
builder.Services.AddScoped<IFeedbackAiService, OllamaFeedbackAiService>();
```

When the endpoint resolved `IFeedbackAiService`, ASP.NET Core used the `AddScoped` registration — constructing `OllamaFeedbackAiService` directly and injecting a blank `HttpClient` with `BaseAddress = null`. Calling `PostAsJsonAsync("/api/generate", ...)` on a client with no BaseAddress throws `InvalidOperationException` immediately. The `catch` block returned `Fallback()` silently. Every submission.

### Fix applied
```csharp
// Single registration — interface and typed client wired together
builder.Services.AddHttpClient<IFeedbackAiService, OllamaFeedbackAiService>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl + "/");  // trailing slash required by HttpClient URI combination
    client.Timeout = TimeSpan.FromSeconds(60);
});
```

The trailing slash on `BaseAddress` was also added. `HttpClient` requires it for relative URI combination to work correctly per RFC 3986.

### What I caught
I identified that the two-step pattern is a common mistake with `IHttpClientFactory`. The fix is non-obvious — the symptom (fallback values) looks like a model issue, not a DI issue.

---

## Phase 11 — Frontend Integration Audit

### What I prompted
"Audit the current backend and give me the exact API contract the frontend must use."

### What the AI found — complete inconsistency list

| # | Problem | Severity |
|---|---------|----------|
| 1 | `api.js` BASE_URL is `http://localhost:5000` — backend runs on `5180` | Fatal |
| 2 | `api.js` uses `/api/feedback` prefix — backend has no `/api` prefix | Fatal |
| 3 | `api.js` calls `PATCH /api/feedback/{id}/status` with body — backend routes are `/action` and `/dismiss` with no body | Fatal |
| 4 | `api.js` calls `GET /api/feedback/stats` — route is `GET /stats` | Fatal |
| 5 | Vite server on port `61814` — CORS allows only `5173` | Fatal |
| 6 | `DashboardView` reads `stats.value.categories` — backend key is `byCategory` | Fatal |
| 7 | `DashboardView` reads `stats.value.priorities` — backend key is `byPriority` | Fatal |
| 8 | `DashboardView` reads `stats.value.statuses` — field does not exist in the response | Fatal |
| 9 | Dashboard CATEGORIES keys are lowercase snake_case — backend returns PascalCase enum names | Fatal |
| 10 | Dashboard PRIORITIES keys are lowercase — backend returns PascalCase | Fatal |

Every single frontend API call was broken. Zero requests would have succeeded.

### Correct contract established
```
Base URL:  http://localhost:5180

POST   /feedback                        body: { "text": "string" }
GET    /feedback                        no body
PATCH  /feedback/{id}/action            no body
PATCH  /feedback/{id}/dismiss           no body
GET    /stats                           no body

Stats response keys: byCategory, byPriority (PascalCase enum values as keys)
Feedback item fields: id, text, summary, category, status, priority, createdAt
```

---

## Phase 12 — Swagger Auto-Launch

### What I prompted
"Make Swagger open automatically when I start the backend from Visual Studio."

### What the AI found
`Program.cs` was already correct — `UseSwagger()` and `UseSwaggerUI()` inside `IsDevelopment()` guard.

`launchSettings.json` had `"launchBrowser": false` on both profiles and no `"launchUrl"` key.

### Fix applied
Added to both profiles:
```json
"launchBrowser": true,
"launchUrl": "swagger"
```

---

## Phase 13 — Documentation

### What I prompted across multiple turns
- "Draft architecture.md"
- "Draft test-strategy.md"
- "Draft risks.md with 5 product-specific risks"
- "Draft discovery-design-pack.md"
- "Draft clarifying-questions.md"

### Files produced
| File | Contents |
|------|----------|
| `docs/architecture.md` | Component diagram, data model, endpoint table, POST flow, AI integration design, hallucination mitigation table, 6 architectural decisions |
| `docs/test-strategy.md` | Unit/integration/E2E strategy, 3-layer AI testing approach, golden-set evaluation design, model drift detection, gap table |
| `docs/risks.md` | 5 product-specific risks grounded in actual code: sync Ollama blocking, immutable classification, no model version tracking, unbounded input, forced single-label classification |
| `docs/discovery-design-pack.md` | Problem statement, user personas, MVP scope, key flows, data model, API surface, stack decisions |
| `docs/clarifying-questions.md` | 10 architecture-relevant questions each with rationale and assumed answer |

### What I steered on documentation
- Asked for risks grounded in actual implementation details, not generic boilerplate
- Asked for the test strategy to clearly separate "implemented tests" from "recommended next tests"
- Asked for honest framing — "out of scope for MVP" rather than implying features exist

---

## Key moments where I corrected the AI

1. **net10.0 → net8.0** — caught immediately from the project file, not from a runtime error
2. **`AddOpenApi()` → Swashbuckle** — wrong API for .NET 8, would fail at startup
3. **`AddHttpClient<Concrete>` + `AddScoped<Interface, Concrete>`** — subtle DI bug that caused silent fallback on every AI call; the symptom looked like a model problem, not a wiring problem
4. **Stats JSON key names** — `byCategory`/`byPriority` vs what the frontend expected (`categories`/`priorities`) — caught during the integration audit
5. **CORS port mismatch** — Vite was configured to port `61814`, CORS only allowed `5173`; this would have caused every browser request to fail with no obvious error message

---

## What was not implemented (honest gaps)

- No input validation on `CreateFeedbackRequest.Text` — no `[MaxLength]`, no `[Required]`
- No manual override for AI classification — `Category` and `Priority` are immutable after creation
- No model version stored on `FeedbackItem`
- No frontend tests (no Vitest, no Playwright)
- No auth of any kind — single-operator localhost tool by design
- E2E tests described in `test-strategy.md` but not implemented
- Golden-set AI evaluation described but not implemented
- `docs/ai-sessions/` had only `README.md` until this file was added
- Empty `Priority.cs` file still exists in `SignalDesk.Domain/Enums/` — stale artifact, safe to delete

---

## Final state

```
dotnet test   →   Total: 14, Passed: 14, Failed: 0
dotnet build  →   Build succeeded. 0 Error(s)
```

Backend starts at `http://localhost:5180`, Swagger opens automatically.  
Frontend runs with `npm run dev` from `Frontend/SignalDesk/SignalDesk/`.
