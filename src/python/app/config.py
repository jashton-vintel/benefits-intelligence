from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    rabbitmq_host: str = "localhost"
    rabbitmq_port: int = 5672
    rabbitmq_user: str
    rabbitmq_password: str


def load_settings() -> Settings:
    # Required fields are populated from the environment, which static analysis can't see.
    return Settings()  # pyright: ignore[reportCallIssue]
