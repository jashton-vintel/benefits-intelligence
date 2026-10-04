import json
import logging
from collections.abc import Generator
from contextlib import contextmanager
from contextvars import ContextVar
from datetime import UTC, datetime
from typing import Any

_correlation_id: ContextVar[str | None] = ContextVar("correlation_id", default=None)
_message_id: ContextVar[str | None] = ContextVar("message_id", default=None)


@contextmanager
def log_context(*, correlation_id: str | None, message_id: str | None) -> Generator[None]:
    correlation_token = _correlation_id.set(correlation_id)
    message_token = _message_id.set(message_id)
    try:
        yield
    finally:
        _message_id.reset(message_token)
        _correlation_id.reset(correlation_token)


class JsonFormatter(logging.Formatter):
    def format(self, record: logging.LogRecord) -> str:
        entry: dict[str, Any] = {
            "timestamp": datetime.fromtimestamp(record.created, UTC).isoformat(),
            "level": record.levelname,
            "logger": record.name,
            "message": record.getMessage(),
        }
        if (correlation_id := _correlation_id.get()) is not None:
            entry["correlation_id"] = correlation_id
        if (message_id := _message_id.get()) is not None:
            entry["message_id"] = message_id
        if record.exc_info:
            entry["exception"] = self.formatException(record.exc_info)
        return json.dumps(entry)


def configure_logging(level: int = logging.INFO) -> None:
    handler = logging.StreamHandler()
    handler.setFormatter(JsonFormatter())
    logging.basicConfig(level=level, handlers=[handler], force=True)

    # The OpenAI SDK's HTTP client logs every request at INFO; keep only its warnings.
    logging.getLogger("httpx2").setLevel(logging.WARNING)
