from datetime import date, datetime
from decimal import Decimal
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field


class ExpenseCreateRequest(BaseModel):
    amount: Decimal = Field(..., gt=0, max_digits=12, decimal_places=2)
    category: str = Field(..., min_length=1, max_length=64)
    description: str = Field(..., min_length=1, max_length=500)
    date: date


class ExpenseBulkCreateRequest(BaseModel):
    expenses: list[ExpenseCreateRequest] = Field(..., min_length=1, max_length=100)


class ExpenseResponse(BaseModel):
    model_config = ConfigDict(from_attributes=True)

    id: UUID
    amount: Decimal
    category: str
    description: str
    date: date
    created_at: datetime


class ExpenseListResponse(BaseModel):
    data: list[ExpenseResponse]
    page: int
    page_size: int
    total_records: int
    total_amount: Decimal


class BulkCreateResponse(BaseModel):
    created_count: int
    ids: list[UUID]
