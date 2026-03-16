# Clarifying Questions

These questions shaped the design decisions for the MVP.

---

## Should AI classification be synchronous?

Assumption: yes.

Reason:
- simpler architecture
- acceptable latency for internal usage

Future improvement: background classification queue.

---

## Should the queue show all items?

Assumption: yes.

The queue functions as both a triage list and audit log.

Future improvement: filter by status.

---

## Can humans override classification?

Assumption: no for MVP.

AI classification is immutable.

Future improvement: allow manual override.

---

## What happens if AI fails?

Fallback classification:

Category: Complaint  
Priority: Medium  
Summary: truncated original text

The system prioritises availability over perfect classification.

---

## Should feedback be deleted?

Assumption: no.

The system behaves as an append‑only log.

Dismissed items represent completed triage.

---

## Is authentication required?

No. MVP assumes a single operator running locally.

---

## Should feedback length be limited?

Yes.

Maximum length: 2000 characters.

This prevents prompt injection and excessive model latency.

---

## Should model version be tracked?

Not required for MVP but recommended.

Future improvement: store model version with each feedback item.