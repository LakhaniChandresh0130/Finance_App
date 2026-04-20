# Expense Tracker (Python + React)

Production-style minimal full-stack implementation for the assignment.

## Stack

- Backend: FastAPI + SQLAlchemy (async) + PostgreSQL (default)
- Frontend: React + Vite

## Key Design Decisions

- **Money safety:** `Decimal` + SQL `Numeric(12,2)` used for amounts.
- **Retry-safe create:** `POST /expenses` requires `Idempotency-Key`; repeated requests with same key return existing record (no duplicate).
- **Real-world scale support:** server-side pagination (`page`, `page_size`) and category filter with indexed columns.
- **Low-latency behavior:** async backend, minimal payloads, response-time header (`x-response-time-ms`), list endpoint computes aggregate in SQL.
- **Security basics:** CORS allowlist, trusted host middleware, secure response headers, strict validation.
- **Bulk ingestion:** `POST /expenses/bulk` with max 100 rows per request.

## Backend Run

1. Open terminal in `expense-tracker-prod/backend`
2. Create virtual env and install:
   - `python -m venv .venv`
   - `.venv\\Scripts\\activate`
   - `pip install -e .`
3. Copy env:
   - `copy .env.example .env`
4. Set `DATABASE_URL` in `.env` for your local PostgreSQL:
   - Example: `postgresql+asyncpg://postgres:YOUR_PASSWORD@localhost:5432/expense_tracker`
   - Ensure database `expense_tracker` exists in your local PostgreSQL instance.
5. Run API:
   - `uvicorn app.main:app --reload --port 8000`
6. Notes:
   - If you installed packages while server was running, stop and restart once (watch reload can get noisy on Windows).

Docs available at: `http://localhost:8000/docs`

## Frontend Run

1. Open terminal in `expense-tracker-prod/frontend`
2. Install + run:
   - `npm install`
   - `npm run dev`
3. Open: `http://localhost:5173`

## API Quick Examples

### Create Expense (idempotent)

`POST /api/v1/expenses` with header `Idempotency-Key: any-unique-string`

```json
{
  "amount": "123.45",
  "category": "Food",
  "description": "Lunch",
  "date": "2026-04-20"
}
```

### List

`GET /api/v1/expenses?category=Food&sort=date_desc&page=1&page_size=20`

### Bulk Insert

`POST /api/v1/expenses/bulk`

```json
{
  "expenses": [
    { "amount": "100.00", "category": "Food", "description": "A", "date": "2026-04-20" },
    { "amount": "300.00", "category": "Travel", "description": "B", "date": "2026-04-19" }
  ]
}
```

## Trade-offs (Timeboxed)

- Chose PostgreSQL as default for production-like behavior and concurrency guarantees.
- Kept auth out to focus on assignment correctness (idempotency, money handling, filtering/sorting, UX behavior under retries).
- Implemented one integration-style test for idempotency path; full test matrix can be expanded if needed.
