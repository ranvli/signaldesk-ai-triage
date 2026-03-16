# Discovery + Design Pack

> One page product thinking summary for the SignalDesk MVP.

---

# Discover

## The core user problem

Customer feedback arrives as unstructured text.  
Reading every message, determining its category, and deciding its priority is slow and inconsistent.

SignalDesk reduces triage time by automatically **classifying and summarising feedback using a local AI model**.

---

## Primary user

Support or product operators responsible for reviewing incoming feedback.

---

## Job to be done

Quickly identify:

- urgent bugs
- feature demand trends
- general sentiment

without manually reading every feedback entry.

---

## What success looks like

An operator can clear the queue in minutes because:

- feedback is auto-classified
- feedback is summarised
- priority is suggested

---

## Existing behaviour replaced

Typical behaviour today:

- reading raw support tickets
- manually tagging feedback
- copying issues to task trackers

SignalDesk replaces this with a **single triage dashboard**.

---

# Design

## Stack decisions

| Decision | Choice | Why |
|---|---|---|
| Backend | .NET 8 Minimal API | Fast to build, minimal boilerplate |
| Frontend | Vue 3 + Vite | Lightweight and reactive |
| AI | Local Ollama LLM | Zero external API dependency |
| Database | SQLite | Simple local persistence |
| Auth | None (MVP) | Single operator local tool |

---

## Most important design tradeoff

**Synchronous AI classification**

Feedback classification runs during POST /feedback.

Pros:
- simple architecture
- no queue or background worker

Cons:
- request latency (≈5–8 seconds depending on model)

Acceptable for an MVP.

---

## Simplifications for MVP

- no authentication
- no assignment workflow
- no manual category override
- single ingestion channel
- synchronous AI inference

---

# Deliver

## In scope

- feedback submission
- AI classification
- AI summarisation
- triage queue
- dashboard stats
- action / dismiss workflow

---

## Out of scope

- multi-user authentication
- feedback ingestion from external systems
- notification systems
- mobile UI

---

## Definition of Done

The system must allow a user to:

1. Submit feedback
2. See the item classified and summarised
3. Review items in a triage queue
4. Action or dismiss items
5. See statistics by category and priority