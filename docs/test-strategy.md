# SignalDesk — Test Strategy

---

## Testing philosophy

Separate deterministic logic from non‑deterministic AI behaviour.

AI integration is isolated behind an interface.

---

## Unit tests

Target:
AI parsing and fallback behaviour.

Tests include:

- malformed JSON responses
- unknown categories
- HTTP errors
- snake_case mapping

---

## Integration tests

Target:
API endpoints and persistence.

Tests include:

- POST /feedback
- GET /feedback
- PATCH action
- PATCH dismiss
- GET stats

SQLite in‑memory database is used for realistic EF Core behaviour.

---

## AI testing

AI outputs cannot be validated with exact text comparisons.

Strategies:

1. Schema validation
2. Golden dataset evaluation
3. Monitoring classification accuracy

---

## End‑to‑end tests

Recommended future tests using Playwright.

Example scenario:

Submit feedback → appears in queue → status updated.

---

## Current coverage

- deterministic behaviour verified
- full API flow tested

---

## Known gaps

- real AI calls excluded from CI
- frontend not tested automatically
- concurrency not tested

Acceptable for MVP scope.