# Finance Data Processing and Access Control — Backend

Production-minded **ASP.NET Core 8** Web API with **PostgreSQL**, **JWT authentication**, **role-based authorization**, **FluentValidation**, **EF Core** (parameterized SQL via LINQ and one raw `SqlQuery` for monthly trends), **health checks**, and **rate limiting** on login.

## Architecture

| Layer | Responsibility |
|--------|----------------|
| `Finance.Domain` | Entities and enums (`User`, `FinancialRecord`, `UserRole`, `TransactionType`) |
| `Finance.Application` | DTOs, validators, application services, repository abstractions |
| `Finance.Infrastructure` | EF Core `FinanceDbContext`, repositories, JWT/password services, dashboard SQL |
| `Finance.Api` | Controllers, policies, middleware, Swagger, startup (migrations + seed) |

## Roles and access

| Role | Dashboard (`/api/dashboard`) | Records (`/api/records`) | Users (`/api/users`) |
|------|------------------------------|--------------------------|------------------------|
| **Viewer** | Read | No access | No access |
| **Analyst** | Read | Read (list, get) | No access |
| **Admin** | Read | Full CRUD (incl. soft delete) | Full user management |

JWT includes `role` and standard `sub` / `nameidentifier` claims. Policies are defined in `PolicyNames`.

## Assumptions

- One global pool of financial records (not multi-tenant per organization). All authenticated **Analyst/Admin** users see the same dataset; **Viewer** sees only aggregated dashboard data.
- Soft delete for records (`IsDeleted`); list queries exclude deleted rows.
- Passwords are hashed with ASP.NET Core `PasswordHasher<T>` (PBKDF2).

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL 14+ (local or Docker)
- Optional: `dotnet-ef` tools (`dotnet tool install --global dotnet-ef --version 8.0.11`)

## Configuration

Edit `src/Finance.Api/appsettings.json` (or user secrets / environment variables):

- **ConnectionStrings:FinanceDatabase** — Npgsql connection string.
- **Jwt** — `Issuer`, `Audience`, `SigningKey` (HMAC key; must be long enough for HS256), `AccessTokenMinutes`.

Environment override for design-time migrations: `FINANCE_DB` (see `DesignTimeDbContextFactory`).

## Database

Create an empty database (e.g. `finance_db`), then from the repo root:

```bash
dotnet ef database update --project src/Finance.Infrastructure/Finance.Infrastructure.csproj --startup-project src/Finance.Api/Finance.Api.csproj
```

On first run, the API applies migrations and seeds demo users (see below).

## Run

```bash
cd src/Finance.Api
dotnet run
```

- Swagger UI: `https://localhost:<port>/swagger` (Development).
- Health: `GET /health`

## Seeded demo accounts

| Email | Password | Role |
|-------|----------|------|
| admin@finance.local | Admin123! | Admin |
| analyst@finance.local | Analyst123! | Analyst |
| viewer@finance.local | Viewer123! | Viewer |

**Login:** `POST /api/auth/login` with `{ "email", "password" }`. Use the returned Bearer token for other endpoints.

### Bulk create records (up to 100 per request)

**Admin only.** `POST /api/records/batch` with JSON body `{ "items": [ ... ] }`. Each element matches a single create payload (`amount`, `type` **0** = Income / **1** = Expense, `category`, `recordDate`, optional `notes`). Returns **200** with `createdCount` and `records` (full DTOs, same order as `items`).

Example (3 rows — you can send up to **100** in `items`):

```json
{
  "items": [
    { "amount": 100.00, "type": 0, "category": "Salary", "recordDate": "2025-01-15", "notes": "Batch 1" },
    { "amount": 25.50, "type": 1, "category": "Coffee", "recordDate": "2025-01-16" },
    { "amount": 500.00, "type": 0, "category": "Bonus", "recordDate": "2025-01-20", "notes": null }
  ]
}
```

More than 100 items → **400** validation error.

**Faster bulk import (smaller response, less JSON serialization):** add `?summaryOnly=true` — response is `createdCount`, `createdIds`, and empty `records` (no repeated DTOs).

```http
POST /api/records/batch?summaryOnly=true
```

## Pagination and performance

- **Records** and **users** list endpoints use **server-side** `Skip` / `Take` with a **total count** query.
- Defaults: `pageNumber = 1`, `pageSize = 10` (capped at **100** in code for records).
- Indexes on `financial_records`: `(IsDeleted, RecordDate)`, `Category`, `Type`, plus FK on `CreatedByUserId`.
- Read paths use **`AsNoTracking()`** where updates are not required.
- List endpoints are suitable for large tables (e.g. 100k+ rows) because only one page is loaded per request.

### Optional: large dataset in PostgreSQL

You can bulk-insert test rows with `generate_series` in SQL (run as admin after noting a valid `CreatedByUserId` from `users`). Example pattern:

```sql
INSERT INTO financial_records ("Id","Amount","Type","Category","RecordDate","Notes","CreatedByUserId","CreatedAtUtc","UpdatedAtUtc","IsDeleted")
SELECT gen_random_uuid(),
       (random() * 5000)::numeric(18,2),
       (random() * 2)::int % 2,
       'Cat' || (random() * 10)::int,
       date '2024-01-01' + (g % 500),
       NULL,
       '<ADMIN_USER_ID>'::uuid,
       now(),
       NULL,
       false
FROM generate_series(1, 100000) g;
```

Replace `<ADMIN_USER_ID>` with a real user id. Re-run `ANALYZE financial_records;` after bulk load.

## Security notes

- **EF Core** translates LINQ to **parameterized** commands (no string concatenation for filters).
- Dashboard **monthly** series uses `Database.SqlQuery` with an **interpolated SQL string** that EF binds as parameters (`from` / `to` dates), not as raw concatenation.
- **Rate limiting** on `POST /api/auth/login` (fixed window; see `Program.cs`).
- Use a strong **JWT signing key** and HTTPS in real deployments.

## API overview

| Method | Route | Auth |
|--------|-------|------|
| POST | `/api/auth/login` | Anonymous |
| GET | `/api/dashboard/summary?from=&to=` | Viewer+ |
| GET | `/api/records` | Analyst+ |
| GET | `/api/records/{id}` | Analyst+ |
| POST/PUT/DELETE | `/api/records` … | Admin |
| GET/POST/PATCH | `/api/users` … | Admin |

## Concurrent edits (optimistic locking)

Financial records use a **version** field so two admins cannot silently overwrite each other’s work.

- Responses from **GET** `/api/records/{id}` and each row in **GET** `/api/records` include **`version`**.
- **PUT** `/api/records/{id}` JSON must include **`expectedVersion`** equal to the version you read. If someone else changed the row first, the API returns **409 Conflict** with `code: CONCURRENCY_CONFLICT`.
- **DELETE** `/api/records/{id}?expectedVersion={n}` — same rule.
- EF Core maps **`Version`** as a **concurrency token**, so concurrent writes that race on the same version still result in only one successful **UPDATE**; the other gets **409**.

**Client flow:** GET → edit → PUT with `expectedVersion` → on 409, GET again and retry or merge.

## Tradeoffs

- **Offset pagination** is simple and indexed; for very deep pages on huge tables, **keyset** pagination can be added later.
- **Monthly trends** raw SQL is kept readable; column names match EF migrations (PascalCase in PostgreSQL).

## API response time (observability and budgets)

Every response can include:

- **`X-Response-Time-Ms`** — milliseconds when the response **starts** (approx. **time-to-first-byte** from the app). It is **not** always equal to total time after a large JSON body is written.
- **`Server-Timing: app;dur=...`** — matches the header above.
- **Slow-request logs** use the **full pipeline** duration (after the request delegate finishes: controller + serialization + compression flush).

**Slow request logging** (structured logs, no alert spam by default):

- **Warning** at ≥ `Performance:SlowRequestWarningThresholdMs` (default **800** ms — upper end of a “good” interactive API band).
- **Error** at ≥ `Performance:SlowRequestErrorThresholdMs` (default **2000** ms).
- **`/health`** is excluded from slow-request logging by default (DB-backed checks can be slower cold).

Tune in `appsettings.json` under **`Performance`**. Set **`DashboardSummaryCacheSeconds`** to **0** to disable in-process caching of dashboard summaries (default **30** s) when you need always-fresh aggregates.

**Compression:** Brotli/Gzip response compression is enabled for HTTPS and JSON/problem+json MIME types (smaller payloads, faster downloads for clients).

### Tests

```bash
dotnet test tests/Finance.Api.Tests/Finance.Api.Tests.csproj
```

- **Middleware tests** (always run): assert headers and trivial pipeline timing.
- **Integration timing test:** set `FINANCE_PERF_TEST=1` with PostgreSQL available (same connection string as the API). It checks login/dashboard/health wall times and that a **second** dashboard call is faster (cache). Budgets are relaxed for dev/CI (e.g. auth &lt; 2 s wall, dashboard &lt; 3 s); tighten locally to match your SLA once the DB is warm.

## License

Submission / assessment use only unless you own the code.
