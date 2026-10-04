from pathlib import Path

from pydantic import SecretStr
from pydantic_settings import BaseSettings, SettingsConfigDict

# Local development defaults; containers supply every value through environment variables.
_REPO_ROOT = Path(__file__).resolve().parents[3]
REPO_ENV_FILE = _REPO_ROOT / ".env"


class OpenAISettings(BaseSettings):
    model_config = SettingsConfigDict(extra="ignore")

    # SecretStr keeps the key out of reprs and logs. Optional here so the rest of the
    # settings stay usable without it; each process refuses to start if it is missing.
    openai_api_key: SecretStr | None = None
    openai_model: str = "gpt-4.1"
    openai_timeout_seconds: float = 60


class Settings(OpenAISettings):
    """Settings for the policy worker."""

    rabbitmq_host: str = "localhost"
    rabbitmq_port: int = 5672
    rabbitmq_user: str
    rabbitmq_password: str
    document_root: Path = _REPO_ROOT / "data"


class ApiSettings(OpenAISettings):
    """Settings for the internal HTTP API. It has no access to RabbitMQ or stored documents."""

    internal_api_key: SecretStr | None = None
    api_host: str = "127.0.0.1"
    api_port: int = 8000


def load_settings(env_file: Path | None = REPO_ENV_FILE) -> Settings:
    # Required fields are populated from the environment, which static analysis can't see.
    return Settings(_env_file=env_file)  # pyright: ignore[reportCallIssue]


def load_api_settings(env_file: Path | None = REPO_ENV_FILE) -> ApiSettings:
    return ApiSettings(_env_file=env_file)
