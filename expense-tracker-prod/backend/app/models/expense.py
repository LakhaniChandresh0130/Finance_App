import uuid
from datetime import date, datetime, timezone
from decimal import Decimal

from sqlalchemy import CheckConstraint, Date, DateTime, Numeric, String, UniqueConstraint
from sqlalchemy.orm import Mapped, mapped_column

from app.db.base import Base


class Expense(Base):
    __tablename__ = "expenses"
    __table_args__ = (
        UniqueConstraint("idempotency_key", name="uq_expense_idempotency_key"),
        CheckConstraint("amount > 0", name="ck_expenses_amount_positive"),
        CheckConstraint("char_length(category) BETWEEN 1 AND 64", name="ck_expenses_category_length"),
        CheckConstraint("char_length(description) BETWEEN 1 AND 500", name="ck_expenses_description_length"),
    )

    id: Mapped[uuid.UUID] = mapped_column(primary_key=True, default=uuid.uuid4)
    amount: Mapped[Decimal] = mapped_column(Numeric(12, 2), nullable=False)
    category: Mapped[str] = mapped_column(String(64), index=True, nullable=False)
    description: Mapped[str] = mapped_column(String(500), nullable=False)
    date: Mapped[date] = mapped_column(Date, index=True, nullable=False)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), default=lambda: datetime.now(timezone.utc), index=True, nullable=False
    )
    idempotency_key: Mapped[str] = mapped_column(String(128), nullable=False)
