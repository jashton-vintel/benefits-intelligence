import pytest
from pydantic import ValidationError

from app.config import load_settings


def test_reads_rabbitmq_settings_from_environment(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("RABBITMQ_USER", "svc-user")
    monkeypatch.setenv("RABBITMQ_PASSWORD", "secret")
    monkeypatch.setenv("RABBITMQ_PORT", "5673")

    settings = load_settings(env_file=None)

    assert settings.rabbitmq_user == "svc-user"
    assert settings.rabbitmq_password == "secret"
    assert settings.rabbitmq_port == 5673
    assert settings.rabbitmq_host == "localhost"


def test_missing_credentials_fail_validation(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.delenv("RABBITMQ_USER", raising=False)
    monkeypatch.delenv("RABBITMQ_PASSWORD", raising=False)

    with pytest.raises(ValidationError):
        load_settings(env_file=None)
