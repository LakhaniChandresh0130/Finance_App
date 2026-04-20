import uuid

from fastapi import APIRouter, Depends, Header, HTTPException, Query, Response, status
from sqlalchemy.ext.asyncio import AsyncSession

from app.core.config import settings
from app.db.session import get_db
from app.repositories.expense_repository import ExpenseRepository
from app.schemas.expense import (
    BulkCreateResponse,
    ExpenseBulkCreateRequest,
    ExpenseCreateRequest,
    ExpenseListResponse,
    ExpenseResponse,
)
from app.services.expense_service import ExpenseService

router = APIRouter(prefix="/expenses", tags=["expenses"])


@router.post("", response_model=ExpenseResponse, status_code=status.HTTP_201_CREATED)
async def create_expense(
    payload: ExpenseCreateRequest,
    response: Response,
    idempotency_key: str | None = Header(default=None, alias="Idempotency-Key"),
    db: AsyncSession = Depends(get_db),
):
    if not idempotency_key:
        raise HTTPException(status_code=400, detail="Idempotency-Key header is required")

    service = ExpenseService(db)
    entity, reused = await service.create_expense_idempotent(payload, idempotency_key)
    if reused:
        response.status_code = status.HTTP_200_OK
    return ExpenseResponse.model_validate(entity)


@router.post("/bulk", response_model=BulkCreateResponse, status_code=status.HTTP_201_CREATED)
async def create_bulk_expenses(payload: ExpenseBulkCreateRequest, db: AsyncSession = Depends(get_db)):
    service = ExpenseService(db)
    prefix = f"bulk-{uuid.uuid4()}"
    entities = await service.create_bulk(payload, prefix)
    return BulkCreateResponse(created_count=len(entities), ids=[e.id for e in entities])


@router.get("", response_model=ExpenseListResponse)
async def list_expenses(
    category: str | None = Query(default=None),
    sort: str = Query(default="date_desc", pattern="^date_desc$"),
    page: int = Query(default=1, ge=1),
    page_size: int = Query(default=settings.default_page_size, ge=1, le=settings.max_page_size),
    db: AsyncSession = Depends(get_db),
):
    repo = ExpenseRepository(db)
    items, total_records, total_amount = await repo.list_expenses(category, sort, page, page_size)
    return ExpenseListResponse(
        data=[ExpenseResponse.model_validate(x) for x in items],
        page=page,
        page_size=page_size,
        total_records=total_records,
        total_amount=total_amount,
    )
