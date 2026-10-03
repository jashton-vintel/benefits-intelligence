from pathlib import Path

from pydantic_settings import BaseSettings, SettingsConfigDict

REPO_ENV_FILE = Path(__file__).resolve().parents[3] / ".env"


class Settings(BaseSettings):
    model_config = SettingsConfigDict(extra="ignore")

    rabbitmq_host: str = "localhost"
    rabbitmq_port: int = 5672
    rabbitmq_user: str
    rabbitmq_password: str


def load_settings(env_file: Path | None = REPO_ENV_FILE) -> Settings:
    # Required fields are populated from the environment, which static analysis can't see.
    return Settings(_env_file=env_file)  # pyright: ignore[reportCallIssue]
