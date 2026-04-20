from httpx import ASGITransport, AsyncClient

from app.db.base import Base
from app.db.session import engine
from app.main import app


async def test_create_expense_idempotent():
    async with engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)

    transport = ASGITransport(app=app)
    async with AsyncClient(transport=transport, base_url="http://test") as client:
        body = {
            "amount": "120.50",
            "category": "Food",
            "description": "Dinner",
            "date": "2026-04-20",
        }
        headers = {"Idempotency-Key": "test-key-123"}
        first = await client.post("/api/v1/expenses", json=body, headers=headers)
        second = await client.post("/api/v1/expenses", json=body, headers=headers)

        assert first.status_code == 201
        assert second.status_code == 200
        assert first.json()["id"] == second.json()["id"]
