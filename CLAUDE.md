# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Green Arcade is a responsive web app for Row-Cycle (Alexandria, Egypt). Members earn points for photo-verified sustainable actions and spend them in Row-Cycle's store (medals, trophies, recycled-aluminium goods, merch). One points ledger connects earning and spending. There will never be separate "game" and "store" points.

## Source of truth

The requirement docs are in `docs/`. Read the relevant ones before changing code. If code and docs disagree, stop and ask; don't silently pick one.

- `docs/BRD.md`: why, MVP scope (§4), open questions (§7)
- `docs/PRD.md`: features F1–F8, roles, screens
- `docs/SRS.md`: FR-xx / NFR-xx IDs, data model (§4), API endpoints (§5), stack
- `docs/IMPLEMENTATION_PLAN.md`: the build order, Steps 0–15. Work on ONE step at a time and check its "Done when" list.
- `docs/DECISIONS.md`: append a row whenever a decision is made. If a decision changes a requirement, edit BRD/PRD/SRS in the same commit.

## Naming and layout

The code is named **RowCycle** (`RowCycle.slnx`, `RowCycle.*` projects and namespaces). The product is still called **Green Arcade** in the docs and UI.

```
RowCycle.slnx, Directory.Build.props (net10.0, nullable, warnings as errors)
backend/src/    RowCycle.Api, RowCycle.Application, RowCycle.Domain, RowCycle.Infrastructure
backend/tests/  RowCycle.Tests  (xUnit + WebApplicationFactory + Testcontainers)
frontend/       green-arcade-web  (Angular, from Step 11: core/, shared/, features/store|account|actions|admin)
```

Dependencies: Api → Application, Infrastructure · Infrastructure → Application → Domain. Infrastructure implements the interfaces Application defines (repositories, `IFileStorage`, `IEmailSender`, the points-balance lock). Domain and Application reference no EF Core / Npgsql / ASP.NET packages.

Cross-cutting pieces already in the Api project:
- Controllers get the `/api/v1` prefix automatically (`Conventions/RoutePrefixConvention.cs`). Write `[Route("products")]`, not `[Route("api/v1/products")]`.
- `Middleware/CorrelationIdMiddleware.cs` sets `X-Correlation-Id`, `HttpContext.TraceIdentifier` and the Serilog `CorrelationId` property. Problem Details responses include it as `correlationId`.
- Exceptions and bare error status codes return RFC 7807 bodies (`AddProblemDetails` + `UseExceptionHandler` + `UseStatusCodePages`).
- `/health` checks PostgreSQL and returns JSON.
- The connection string is `ConnectionStrings:Postgres`. `RowCycle.Infrastructure.DependencyInjection.GetConnectionString` fails fast if it's missing.

Integration tests use `[Collection(ApiCollection.Name)]` to share one `ApiFactory` (`backend/tests/RowCycle.Tests/Infrastructure/ApiFactory.cs`), which starts one Postgres container per test run. Inject settings with `builder.UseSetting`, not `ConfigureAppConfiguration`: `Program.cs` reads configuration before the latter is applied.

## Commands

The SDK is pinned to .NET 10.0.400 in `global.json`. Docker must be running for the DB and for tests.

```bash
docker compose up -d db                                            # dev DB; credentials come from .env (gitignored)
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=<db>;Username=<user>;Password=<pw>" --project backend/src/RowCycle.Api   # once
dotnet build RowCycle.slnx
dotnet run --project backend/src/RowCycle.Api                      # http://localhost:5130, Swagger at /swagger (Development only)
dotnet test RowCycle.slnx                                          # all tests
dotnet test RowCycle.slnx --filter "FullyQualifiedName~HealthTests"   # one class or test
docker compose up --build                                          # api on :8080 + db
```

From Step 2 (EF Core) and Step 11 (Angular):

```bash
dotnet ef migrations add <Name> -p backend/src/RowCycle.Infrastructure -s backend/src/RowCycle.Api
dotnet ef database update     -p backend/src/RowCycle.Infrastructure -s backend/src/RowCycle.Api
cd frontend/green-arcade-web && npm start
```

## Stack (fixed — do not substitute)

- ASP.NET Core Web API on .NET 10, clean architecture as above
- EF Core + Npgsql, code-first migrations, `snake_case` names via `EFCore.NamingConventions`
- PostgreSQL (Docker for dev); browse it with pgAdmin or DBeaver
- ASP.NET Core Identity with `Guid` keys, JWT (15 min) + rotating refresh tokens (7 days); roles Member, Moderator, StoreManager, Admin
- FluentValidation, Serilog, OpenAPI
- Angular (latest stable, standalone components, signals), mobile-first from 360 px; UI text in translation files so Arabic/RTL can be added later
- xUnit + Testcontainers (real PostgreSQL) for integration tests

## Rules that must never be broken

1. Points change ONLY through the points ledger service (`IPointsService`: Earn / Redeem / Reverse / Adjust). Every change writes one append-only `points_ledger` row and updates `user_profiles.points_balance` in the SAME transaction, under a row lock on the balance. Never update or delete ledger rows. The balance never goes below 0, and the `CHECK` constraint is the last line of defence. (FR-05, FR-06)
2. Checkout, submission approval, order cancellation and order delivery (which writes the product reward points Earn row) each run in ONE database transaction covering the order, stock, ledger and status history. (FR-10, FR-14, FR-17, FR-18, NFR-05)
3. Domain and Application never reference EF Core, Npgsql or ASP.NET. Row locking and transaction details live in Infrastructure.
4. All times are UTC (`timestamptz`); money is `numeric(12,2)` EGP; ids are `uuid`.
5. Order items snapshot prices at checkout (FR-15). Order status changes only along Placed → Confirmed → Shipped → Delivered, or to Cancelled from Placed/Confirmed, and every change is logged (FR-16).
6. API base path is `/api/v1`. Errors return RFC 7807 Problem Details. Lists return `{ items, page, pageSize, total }`. Admin writes are audit-logged (FR-19).
7. No secrets in code or committed appsettings; use user-secrets or env vars.
8. Don't build anything BRD §4 lists as Release 2, Release 3, Later or Removed. The data model only keeps room for those (e.g. `products.product_type`, `submissions.weight_kg`).

## How to work

- Start each plan step in plan mode: list the files to create or change and the FR/NFR IDs they satisfy, then wait for approval.
- Put requirement IDs in commit messages, e.g. `feat(points): ledger service (FR-05, FR-06)`.
- A step is finished when the build passes, tests pass, the migration is applied, and you've given a short summary of what was done and what's next.
- If a requirement is ambiguous, or an open question in BRD §7 blocks you, ask instead of guessing. IMPLEMENTATION_PLAN Step 0 lists defaults you can use while a decision is still pending.
