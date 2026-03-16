# SignalDesk — Discovery & Design Pack

## Problem

Teams receive large volumes of unstructured feedback.  
Important signals like bugs or feature demand are buried inside free‑text messages.

SignalDesk uses a **local AI model** to classify and summarise feedback automatically.

---

## Core Idea

Turn raw feedback into structured triage items:

- summary
- category
- priority

Operators then review a clean queue instead of reading raw text.

---

## Queue Design

Each queue item displays:

- AI summary
- category badge
- priority badge
- status
- option to view original feedback

Example:

Bug · High  
Exporting invoices fails intermittently

View full feedback

Original feedback:  
After the last update we noticed exporting invoices fails...

The summary reduces cognitive load while preserving full context.

---

## Dashboard

Aggregated counts:

By category:

- Bug
- Feature Request
- Complaint
- Praise

By priority:

- High
- Medium
- Low

This allows product teams to detect trends quickly.

---

## AI Design

AI runs locally via Ollama.

Classification returns:

- summary
- category
- priority

Closed vocabulary prevents hallucinated categories.

Fallback behaviour ensures the API never fails.

---

## MVP Scope

- feedback submission
- AI classification
- triage queue
- dashboard stats
- action / dismiss workflow

---

## Future Improvements

- asynchronous classification queue
- manual classification override
- multi‑label tagging
- model version tracking
- authentication