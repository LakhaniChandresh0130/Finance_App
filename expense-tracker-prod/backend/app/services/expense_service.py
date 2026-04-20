from sqlalchemy.exc import IntegrityError
from sqlalchemy.ext.asyncio import AsyncSession

from app.repositories.expense_repository import ExpenseRepository
from app.schemas.expense import ExpenseBulkCreateRequest, ExpenseCreateRequest


class ExpenseService:
    def __init__(self, db: AsyncSession) -> None:
        self.repo = ExpenseRepository(db)

    async def create_expense_idempotent(self, payload: ExpenseCreateRequest, idempotency_key: str):
        existing = await self.repo.get_by_idempotency_key(idempotency_key)
        if existing:
            return existing, True

        try:
            created = await self.repo.create(payload, idempotency_key)
            return created, False
        except IntegrityError:
            existing = await self.repo.get_by_idempotency_key(idempotency_key)
            if existing:
                return existing, True
            raise

    async def create_bulk(self, payload: ExpenseBulkCreateRequest, idempotency_prefix: str):
        return await self.repo.create_bulk(payload.expenses, idempotency_prefix)
