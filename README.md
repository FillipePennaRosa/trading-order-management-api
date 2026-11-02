# Trading Order Management API

**Original demonstration project for a software engineering portfolio.**

A small ASP.NET Core API for *simulated* limit orders. It shows C#/.NET, layering, business invariants, an idempotency contract, database constraints, automated tests and continuous integration. It **does not execute real trades**, connect to brokerages, or handle funds.

## Stack

- .NET 10 / ASP.NET Core Minimal APIs
- EF Core 10 and SQLite (local/demo persistence only)
- xUnit for unit and API integration tests
- GitHub Actions CI (`build` and `test`)

## Architecture

```text
src/
  TradingOrders.Domain/           # Order model, validation, transitions
  TradingOrders.Application/      # Use cases, idempotency, abstractions
  TradingOrders.Infrastructure/   # SQLite/EF Core, unique constraints
  TradingOrders.Api/              # HTTP endpoints and composition root
tests/
  TradingOrders.Tests/            # Domain, application, HTTP integration
```

For trade-offs and limitations, see [docs/architecture.md](docs/architecture.md).

## Run locally

Install [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet restore src/TradingOrders.Api/TradingOrders.Api.csproj
dotnet run --project src/TradingOrders.Api/TradingOrders.Api.csproj --urls http://localhost:5080
```

The local SQLite database is created on first run. A small OpenAPI document is served at `/openapi/v1.json` when running in `Development`.

Create an order:

```bash
curl -i -X POST http://localhost:5080/api/orders \
  -H 'Content-Type: application/json' \
  -d '{"clientOrderId":"demo-buy-001","symbol":"MSFT","side":"Buy","quantity":2,"limitPrice":350.50}'
```

- First request: **201 Created**.
- Retry with the same `clientOrderId` and normalized economics: **200 OK**, same order ID.
- Retry with that key but a different quantity/price/side/symbol: **409 Conflict**.

List: `GET /api/orders?limit=20`
Read: `GET /api/orders/{id}`
Cancel: `POST /api/orders/{id}/cancel` (idempotent)
Health: `GET /health`

## Tests

```bash
dotnet test tests/TradingOrders.Tests/TradingOrders.Tests.csproj
```

CI runs on pushes and PRs to `main`. Before showing this as a verified engineering sample, **run the tests and confirm a green GitHub Actions build**. The source archive was generated in an environment without the .NET SDK, so a successful compile/test is **not claimed**.

## Engineering and security limitations

- Sample only, never use with real financial accounts.
- No authentication or authorization; production use would require both.
- SQLite demo storage serializes decimal quantities/prices as invariant-culture TEXT to preserve exact round-trips. SQL numeric sorting/arithmetic on those fields is not supported here. A production financial system must use exact numeric database columns and precision-boundary testing.
- Database schema bootstraps with `EnsureCreatedAsync`; real systems need versioned migrations.
- No external secrets, real customer data or proprietary code included.

## Author and licensing

Independent portfolio demonstration prepared for Fillipe Penna Rosa. No public license has been selected yet; choose one consciously before enabling third-party reuse. This demo contains no proprietary client code.
