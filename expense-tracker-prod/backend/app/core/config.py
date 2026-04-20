from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    app_name: str = "Expense Tracker API"
    app_env: str = "dev"
    database_url: str = "postgresql+asyncpg://postgres:123456@localhost:5432/finance_db"
    api_prefix: str = "/api/v1"
    cors_origins: str = "http://localhost:5173"
    default_page_size: int = 20
    max_page_size: int = 100
    idempotency_ttl_hours: int = 24

    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8")


settings = Settings()
