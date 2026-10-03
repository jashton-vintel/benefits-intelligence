import json
import logging
import sys
from typing import Any

import pytest

from app.logging_config import JsonFormatter, log_context


def format_record(message: str, *, exc_info: bool = False) -> dict[str, Any]:
    record = logging.LogRecord(
        name="test",
        level=logging.INFO,
        pathname=__file__,
        lineno=1,
        msg=message,
        args=None,
        exc_info=None,
    )
    if exc_info:
        try:
            raise ValueError("boom")
        except ValueError:
            record.exc_info = sys.exc_info()
    return json.loads(JsonFormatter().format(record))


def test_includes_context_fields_inside_log_context() -> None:
    with log_context(correlation_id="corr-1", message_id="msg-1"):
        entry = format_record("inside")

    assert entry["message"] == "inside"
    assert entry["level"] == "INFO"
    assert entry["correlation_id"] == "corr-1"
    assert entry["message_id"] == "msg-1"


def test_context_fields_are_cleared_when_context_exits() -> None:
    with log_context(correlation_id="corr-1", message_id="msg-1"):
        pass

    entry = format_record("outside")

    assert "correlation_id" not in entry
    assert "message_id" not in entry


def test_context_fields_are_cleared_when_body_raises() -> None:
    with pytest.raises(RuntimeError), log_context(correlation_id="corr-1", message_id="msg-1"):
        raise RuntimeError

    entry = format_record("after failure")

    assert "correlation_id" not in entry


def test_nested_context_restores_outer_values() -> None:
    with log_context(correlation_id="outer", message_id="outer-msg"):
        with log_context(correlation_id="inner", message_id="inner-msg"):
            assert format_record("inner")["correlation_id"] == "inner"

        assert format_record("outer")["correlation_id"] == "outer"


def test_includes_exception_details() -> None:
    entry = format_record("failed", exc_info=True)

    assert "ValueError: boom" in entry["exception"]
