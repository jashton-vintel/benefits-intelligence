import json
from pathlib import Path
from typing import Any

import pytest
from pydantic import BaseModel, ValidationError

from app.models.messages import (
    ProcessPolicyCompleted,
    ProcessPolicyFailed,
    ProcessPolicyRequested,
)

FIXTURES = Path(__file__).resolve().parents[3] / "contracts" / "fixtures"


def read_fixture(name: str) -> str:
    return (FIXTURES / name).read_text(encoding="utf-8")


@pytest.mark.parametrize(
    ("fixture", "model"),
    [
        ("process_requested.json", ProcessPolicyRequested),
        ("process_completed.json", ProcessPolicyCompleted),
        ("process_failed.json", ProcessPolicyFailed),
    ],
)
def test_contract_fixture_round_trips(fixture: str, model: type[BaseModel]) -> None:
    raw = read_fixture(fixture)

    message = model.model_validate_json(raw)

    assert message.model_dump(mode="json") == json.loads(raw)


def test_requested_rejects_unsupported_schema_version() -> None:
    payload: dict[str, Any] = json.loads(read_fixture("process_requested.json"))
    payload["schema_version"] = "2.0"

    with pytest.raises(ValidationError):
        ProcessPolicyRequested.model_validate(payload)
