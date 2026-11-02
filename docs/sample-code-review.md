# Illustrative code review (English)

This is a **sample review exercise**, not a review posted to an actual teammate's PR.

**Scenario:** A contributor proposes creating a simulated order by calling `GetByClientOrderIdAsync` followed by `Add` with no database uniqueness constraint.

> **Blocking — idempotency is not concurrency-safe.** Two HTTP requests using the same `clientOrderId` can both pass the lookup before either writes. This can create two orders with the same external key. Please add a unique constraint at the persistence layer, treat a uniqueness violation as a retry candidate, re-read the existing order, then return 200 only when its economic fields match the submitted payload. Return 409 when the key is reused with different order parameters. Add an integration test that demonstrates the retry contract, and a concurrency test against the actual persistence provider.

**Why:** An application-level existence check is useful for the common path but is insufficient under concurrency. The uniqueness guarantee must live in the database. The service should not silently accept a different order for the same external reference.

**Trade-off:** In a high-throughput production trading system this would require a deeper design (DB transaction guarantees, isolation, request tracing, message deduplication, recovery policies and domain-specific broker semantics). This portfolio example deliberately stays small.
