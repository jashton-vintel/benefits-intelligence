from typing import Any

import pytest

from app.config import Settings
from app.messaging import rabbit_connection

SETTINGS = Settings.model_validate({"rabbitmq_user": "svc", "rabbitmq_password": "secret"})


class FlakyBroker:
    def __init__(self, failures: int) -> None:
        self.failures = failures
        self.calls = 0

    async def connect_robust(self, **_: Any) -> str:
        self.calls += 1
        if self.calls <= self.failures:
            raise ConnectionRefusedError("Connect call failed")
        return "connection"


@pytest.fixture
def no_waiting(monkeypatch: pytest.MonkeyPatch) -> list[float]:
    delays: list[float] = []

    async def sleep(delay: float) -> None:
        delays.append(delay)

    monkeypatch.setattr(rabbit_connection.asyncio, "sleep", sleep)
    return delays


async def test_retries_until_the_broker_accepts_connections(
    monkeypatch: pytest.MonkeyPatch, no_waiting: list[float]
) -> None:
    broker = FlakyBroker(failures=3)
    monkeypatch.setattr(rabbit_connection.aio_pika, "connect_robust", broker.connect_robust)

    connection = await rabbit_connection.connect(SETTINGS, attempts=5, first_delay=1)

    assert connection == "connection"
    assert broker.calls == 4
    assert no_waiting == [1, 2, 4]


async def test_gives_up_after_the_last_attempt(
    monkeypatch: pytest.MonkeyPatch, no_waiting: list[float]
) -> None:
    broker = FlakyBroker(failures=10)
    monkeypatch.setattr(rabbit_connection.aio_pika, "connect_robust", broker.connect_robust)

    with pytest.raises(ConnectionRefusedError):
        await rabbit_connection.connect(SETTINGS, attempts=3, first_delay=1)

    assert broker.calls == 3
    assert no_waiting == [1, 2]
