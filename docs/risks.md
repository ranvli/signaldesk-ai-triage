# SignalDesk — Product Risks

---

## Risk 1 — AI latency

AI classification runs synchronously.

Impact:
slow feedback submission.

Mitigation:
future async classification queue.

---

## Risk 2 — Incorrect classification

AI may misclassify feedback.

Mitigation:

- closed vocabulary categories
- fallback behaviour
- potential manual override feature

---

## Risk 3 — Model drift

Changing the AI model may alter classification behaviour.

Mitigation:

store model version with each feedback item.

---

## Risk 4 — Prompt injection

User feedback is inserted directly into the prompt.

Mitigation:

- input length limit
- strict output schema
- closed category vocabulary

---

## Risk 5 — Single label limitation

Feedback may contain multiple signals.

Example:

Export crashes and also please add CSV support.

Current design forces a single category.

Future improvement:
multi‑label tagging system.