from decimal import Decimal

from sqlalchemy import Select, func, select
from sqlalchemy.ext.asyncio import AsyncSession

from app.models.expense import Expense
from app.schemas.expense import ExpenseCreateRequest


class ExpenseRepository:
    def __init__(self, db: AsyncSession) -> None:
        self.db = db

    async def create(self, payload: ExpenseCreateRequest, idempotency_key: str) -> Expense:
        expense = Expense(
            amount=payload.amount,
            category=payload.category.strip(),
            description=payload.description.strip(),
            date=payload.date,
            idempotency_key=idempotency_key,
        )
        self.db.add(expense)
        await self.db.commit()
        await self.db.refresh(expense)
        return expense

    async def get_by_idempotency_key(self, idempotency_key: str) -> Expense | None:
        stmt = select(Expense).where(Expense.idempotency_key == idempotency_key).limit(1)
        result = await self.db.execute(stmt)
        return result.scalar_one_or_none()

    async def create_bulk(self, expenses: list[ExpenseCreateRequest], idempotency_prefix: str) -> list[Expense]:
        entities = [
            Expense(
                amount=e.amount,
                category=e.category.strip(),
                description=e.description.strip(),
                date=e.date,
                idempotency_key=f"{idempotency_prefix}:{idx}",
            )
            for idx, e in enumerate(expenses)
        ]
        self.db.add_all(entities)
        await self.db.commit()
        return entities

    async def list_expenses(self, category: str | None, sort: str, page: int, page_size: int) -> tuple[list[Expense], int, Decimal]:
        filters = []
        if category:
            filters.append(Expense.category == category)

        base: Select[tuple[Expense]] = select(Expense).where(*filters)
        order_by = Expense.date.desc() if sort == "date_desc" else Expense.created_at.desc()
        stmt = base.order_by(order_by).offset((page - 1) * page_size).limit(page_size)
        items = (await self.db.execute(stmt)).scalars().all()

        total_count_stmt = select(func.count(Expense.id)).where(*filters)
        total_amount_stmt = select(func.coalesce(func.sum(Expense.amount), 0)).where(*filters)

        total_records = (await self.db.execute(total_count_stmt)).scalar_one()
        total_amount = (await self.db.execute(total_amount_stmt)).scalar_one()
        return items, int(total_records), Decimal(total_amount)
