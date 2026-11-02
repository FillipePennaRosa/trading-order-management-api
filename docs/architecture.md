# Architecture and trade-offs

## Project structure

- **Domain:** order invariant enforcement and state transitions; no infrastructure dependency.
- **Application:** use cases, idempotency policy and repository interface; independent of EF Core.
- **Infrastructure:** EF Core SQLite persistence, uniqueness constraints, order repository.
- **API:** lightweight Minimal API endpoints, HTTP mapping, DI and demo initialization.
- **Tests:** domain/application unit tests, HTTP integration tests against disposable SQLite databases.

## Explicit decisions

1. **No broker/execution engine.** Pending means accepted by the simulator, not routed or filled.
2. **One client-generated idempotency key.** Reusing `clientOrderId` with the same normalized order parameters replays the same resource; changing the economics returns 409.
3. **Database uniqueness is authoritative.** The application checks for duplicates first but relies on the DB unique index under concurrency. A unique-index race is re-read, then classified as a replay or conflict.
4. **Cancellation is idempotent.** Repeated cancellation returns the existing cancelled resource; does not change its timestamp.
5. **Simple, intentional layering.** Four assemblies are enough for this portfolio exercise. No CQRS bus, distributed locks, microservices or repositories per aggregate.
6. **Demo-only persistence.** `EnsureCreatedAsync` makes onboarding simple; production would use migrations, stronger observability, authn/authz, rate limiting, audit trail, and an explicit deployment process.
7. **SQLite precision caveat.** SQLite has no native decimal storage. This demo serializes decimal values to invariant-culture TEXT to preserve their base-10 value exactly on round-trips. It intentionally does not perform SQL arithmetic, numeric sorting, or filtering on those columns. For a real-money system, use a database supporting exact numeric columns (such as SQL Server `decimal(19, 8)`), with explicit scale, range and precision-boundary tests.
8. **No auth.** Intentional for an educational simulator, unsuitable for production.

## Review checklist

- Are REST status codes and retry semantics clear?
- Are decimal precision and database constraints appropriate to the deployment?
- What are the expected behaviors on duplicated, reordered, or concurrent requests?
- Are tests exercising business invariants and idempotent retries?
- Can this design be explained without depending on AI-generated text?

## AI-assisted workflow (truthful portfolio presentation)

You may describe use of AI assistants only if you actually use them in your workflow, review their changes, and can explain all the implementation choices. Do not imply automatic review or testing if neither happened.
