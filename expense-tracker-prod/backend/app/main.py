import asyncio
import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.middleware.trustedhost import TrustedHostMiddleware
from sqlalchemy.exc import SQLAlchemyError

from app.api.expenses import router as expenses_router
from app.core.config import settings
from app.db.base import Base
from app.db.session import engine
from app.middleware.response_time import ResponseTimeMiddleware
from app.models.expense import Expense  # noqa: F401

logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(_: FastAPI):
    max_attempts = 15
    retry_delay_seconds = 2
    last_error: Exception | None = None

    for attempt in range(1, max_attempts + 1):
        try:
            async with engine.begin() as conn:
                await conn.run_sync(Base.metadata.create_all)
            logger.info("Database connection established on startup")
            break
        except SQLAlchemyError as exc:
            last_error = exc
            logger.warning(
                "Database startup attempt %s/%s failed. Retrying in %ss.",
                attempt,
                max_attempts,
                retry_delay_seconds,
            )
            await asyncio.sleep(retry_delay_seconds)
    else:
        raise RuntimeError(
            "Could not connect to PostgreSQL after startup retries. "
            "Ensure local PostgreSQL is running and DATABASE_URL credentials are correct."
        ) from last_error

    yield


app = FastAPI(title=settings.app_name, lifespan=lifespan)

app.add_middleware(
    CORSMiddleware,
    allow_origins=[x.strip() for x in settings.cors_origins.split(",") if x.strip()],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)
app.add_middleware(TrustedHostMiddleware, allowed_hosts=["localhost", "127.0.0.1", "*.localhost", "test"])
app.add_middleware(ResponseTimeMiddleware)

app.include_router(expenses_router, prefix=settings.api_prefix)


@app.middleware("http")
async def secure_headers(request: Request, call_next):
    response = await call_next(request)
    response.headers["X-Content-Type-Options"] = "nosniff"
    response.headers["X-Frame-Options"] = "DENY"
    response.headers["Referrer-Policy"] = "strict-origin-when-cross-origin"
    return response


@app.get("/health")
async def health():
    return {"status": "ok"}
