# SignalDesk — Architecture

## Overview

SignalDesk is a lightweight AI-assisted feedback triage tool. Incoming customer feedback is
classified and summarised by a local LLM on submission; operators then action or dismiss items
through a queue interface.

```
┌─────────────────────┐        HTTP / JSON         ┌──────────────────────────────┐
│   Vue 3 frontend    │ ◄────────────────────────► │   .NET 8 Minimal API         │
│   localhost:5173    │                             │   SignalDesk.Api             │
└─────────────────────┘                             └──────────┬───────────────────┘
                                                               │
                                          ┌────────────────────┼────────────────────┐
                                          │                    │                    │
                                 ┌────────▼──────┐   ┌────────▼──────┐   ┌─────────▼──────┐
                                 │  SignalDesk   │   │  SignalDesk   │   │  Ollama        │
                                 │  .Domain      │   │  .Infra       │   │  localhost     │
                                 │  entities     │   │  EF Core      │   │  :11434        │
                                 │  enums        │   │  SQLite       │   │                │
                                 └───────────────┘   └───────────────┘   └────────────────┘
```

---

## Project Structure

| Project | Responsibility |
|---|---|
| `SignalDesk.Domain` | `FeedbackItem` entity, `FeedbackCategory`, `FeedbackStatus`, `FeedbackPriority` enums. No external dependencies. |
| `SignalDesk.Infrastructure` | EF Core `SignalDeskDbContext`, `FeedbackRepository`, `OllamaFeedbackAiService`. All I/O lives here. |
| `SignalDesk.Api` | Minimal API route registration, DTOs, DI wiring, `Program.cs`. Thin by design — no business logic. |
| `SignalDesk.Tests` | xUnit integration tests (via `WebApplicationFactory`) and unit tests for AI fallback behaviour. |

Dependency direction: `Api` → `Infrastructure` → `Domain`. The domain has zero external references.

---

## Data Model

```
FeedbackItem
├── Id          Guid          primary key, assigned at creation
├── Text        string        raw customer input, required
├── Summary     string        one-sentence AI summary, required (fallback: truncated Text)
├── Category    FeedbackCategory  { Bug, FeatureRequest, Complaint, Praise }
├── Status      FeedbackStatus    { Open, Actioned, Dismissed }
├── Priority    FeedbackPriority  { Low, Medium, High }
└── CreatedAt   DateTime      UTC, set at creation, never mutated
```

All three enums are stored as their string names in SQLite (EF `HasConversion<string>()`).
This keeps the raw database human-readable and prevents silent data corruption when enum
members are reordered.

`Id` is a client-generated `Guid` (`Guid.NewGuid()` at the API layer) rather than an
auto-increment integer. This avoids a round-trip to the database before the insert and keeps
IDs opaque to the caller.

`CreatedAt` is the only field that is never updated after creation.
`Status` is the only field mutated post-creation (via the action/dismiss endpoints).
Classification fields (`Summary`, `Category`, `Priority`) are immutable once written.

---

## API

All responses are JSON. Enums serialise as strings (`JsonStringEnumConverter` registered
globally). No envelope wrapper — the resource is returned directly.

| Method | Path | Description |
|---|---|---|
| `POST` | `/feedback` | Submit text; triggers AI classification; returns `201 Created` with the full item |
| `GET` | `/feedback` | Returns all items ordered by `createdAt` desc |
| `PATCH` | `/feedback/{id}/action` | Sets `status = Actioned`; returns updated item |
| `PATCH` | `/feedback/{id}/dismiss` | Sets `status = Dismissed`; returns updated item |
| `GET` | `/stats` | Returns `{ byCategory, byPriority }` counts across all items |

Route constraint `{id:guid}` is used on PATCH routes — a non-Guid path segment returns `404`
from the routing layer before reaching the handler.

---

## Component Interactions on POST /feedback

```
Client
  │  POST /feedback { "text": "..." }
  ▼
FeedbackEndpoints.CreateAsync
  │  await ai.AnalyzeAsync(text)
  ▼
OllamaFeedbackAiService
  │  POST http://localhost:11434/api/generate
  │  { model, system: <SystemPrompt>, prompt: text, stream: false }
  ▼
Ollama (local)
  │  { "response": "{\"summary\":\"...\",\"category\":\"bug\",\"priority\":\"high\"}" }
  ▼
OllamaFeedbackAiService  (parse → map → validate)
  │  AiAnalysisResult { Summary, Category, Priority }
  ▼
FeedbackEndpoints.CreateAsync
  │  new FeedbackItem { ..., Status = Open, CreatedAt = UtcNow }
  │  repo.Add(item) → repo.SaveChangesAsync()
  ▼
Client  201 Created  FeedbackResponse
```

---

## AI Integration Design

The AI layer is intentionally isolated behind `IFeedbackAiService`. The rest of the
application never touches Ollama directly — it receives a typed `AiAnalysisResult` record
regardless of what happened upstream.

**System / prompt split**
The Ollama request separates concerns:
- `system`: the role definition, allowed values, classification rules, and output schema. Static across all requests.
- `prompt`: the raw feedback text only. No instructions mixed in.

This mirrors the intended usage of the Ollama API and produces more consistent output than
embedding instructions inside the user prompt.

**Category normalisation**
The system prompt instructs the model to return snake_case categories (`feature_request`).
`Enum.TryParse` alone cannot map `feature_request` → `FeatureRequest`. An explicit
`Dictionary<string, string>` maps every expected model output to its C# enum name before
parsing. Unknown values fall through to the fallback.

**Fallback contract**
If anything in the pipeline fails — HTTP error, malformed JSON, unrecognised enum value,
timeout — `AnalyzeAsync` returns a deterministic fallback:

```
Summary  = original text truncated to 120 characters
Category = Complaint
Priority = Medium
```

The `POST /feedback` endpoint never returns a 5xx due to Ollama being unavailable. The item
is always saved. This was a deliberate availability-over-consistency trade-off for the MVP.

---

## Hallucination Mitigation

LLMs are non-deterministic. The following layers reduce the impact of unexpected output:

| Layer | Mechanism |
|---|---|
| Prompt design | Closed-vocabulary outputs: model is given exact allowed values and told to return nothing else |
| Prompt design | Explicit classification rules reduce ambiguity (`if broken behaviour → bug`, etc.) |
| Prompt design | `system` / `prompt` separation keeps instructions out of the user-controlled input |
| Output parsing | `PropertyNameCaseInsensitive = true` tolerates minor casing variation |
| Output parsing | `StripCodeFences()` handles models that wrap JSON in ` ```json ` blocks despite instructions |
| Output parsing | `_categoryMap` dictionary handles snake_case vs PascalCase mismatch |
| Output parsing | `Enum.TryParse` with `ignoreCase: true` as a second-pass tolerance layer |
| Fallback | Any failure at any point produces a known-safe result; no exception propagates to the caller |

What is **not** mitigated at MVP: prompt injection via feedback text, semantic drift in
long-running deployments, or model version inconsistency across environments. These are
acceptable risks for a single-operator internal tool.

---

## Auth Approach (MVP)

There is no authentication or authorisation in this MVP. The API accepts requests from
`http://localhost:5173` only (CORS policy — `WithOrigins`, not `AllowAnyOrigin`). This is
a deliberate scope cut, not an oversight.

The assumed deployment context is a single-operator internal tool running on localhost.
`AllowAnyOrigin` was explicitly rejected in favour of a pinned origin to avoid establishing
a permissive baseline that gets forgotten in production.

**If auth were to be added**, the natural insertion point is ASP.NET Core middleware before
the route group. `IFeedbackRepository` and the endpoints themselves require no changes — the
authentication concern is fully orthogonal to the application logic.

---

## Key Architectural Decisions

**1. Repository over direct DbContext in endpoints**
Endpoints depend on `IFeedbackRepository`, not `SignalDeskDbContext`. This keeps EF Core
contained to the Infrastructure project. The integration tests swap the real SQLite file for
an in-memory SQLite connection by replacing a single DI registration.

**2. IEntityTypeConfiguration over fluent API in OnModelCreating**
EF configuration uses the `IEntityTypeConfiguration<T>` pattern with
`ApplyConfigurationsFromAssembly`. Adding a new entity means dropping a new configuration
file — `SignalDeskDbContext` itself never changes.

**3. IDesignTimeDbContextFactory in Infrastructure**
`SignalDeskDbContextFactory` lets EF tooling (`dotnet ef migrations add`) run from the
Infrastructure project directly, without specifying a startup project. Migrations are
currently optional (the API calls `EnsureCreated()` at startup) but the project is
structured to support them without any scaffolding changes.

**4. Typed AiAnalysisResult over tuple return**
`Task<AiAnalysisResult>` is returned from `IFeedbackAiService` rather than a tuple. Named
properties make call sites self-documenting and the type is easily extended (add a
`ConfidenceScore` later, for example) without changing the interface signature.

**5. Enum-as-string storage**
All three enums are stored as strings in SQLite. Integer ordinals are cheaper but fragile —
reordering or inserting enum members silently corrupts historical data. String values make
migrations and raw database inspection predictable.

**6. Classification is immutable post-creation**
`Category`, `Priority`, and `Summary` are set once at creation and never updated. Status
(`Open` → `Actioned` / `Dismissed`) is the only mutable field. This simplifies the data
model, avoids concurrency concerns, and makes the audit trail unambiguous.
