import json
from pathlib import Path
from typing import Any

import pytest

from app.models.document import DocumentChunk
from app.models.extraction import ExtractedPolicy

SAMPLE_DATA = Path(__file__).resolve().parents[3] / "sample-data"


def read_expected(name: str) -> dict[str, Any]:
    return json.loads((SAMPLE_DATA / "expected" / f"{name}.json").read_text(encoding="utf-8"))


def model_output_from_expected(name: str, confidence: float = 0.9) -> ExtractedPolicy:
    """The answer a perfect model would give for a sample policy, quotes included."""
    output: dict[str, Any] = {}
    for path, fact in read_expected(name)["facts"].items():
        value = fact["value"]
        if path.startswith("coverage.") and value is not None:
            value = {"session_limit": None} | value

        group, _, field = path.rpartition(".")
        target = output.setdefault(group, {}) if group else output
        target[field] = {
            "value": value,
            "quote": fact["evidence"],
            "confidence": confidence,
            "ambiguous": fact.get("ambiguous", False),
        }
    return ExtractedPolicy.model_validate(output)


class FakeExtractor:
    def __init__(self, result: ExtractedPolicy) -> None:
        self.result = result
        self.received: list[DocumentChunk] = []

    async def extract(self, chunks: list[DocumentChunk]) -> ExtractedPolicy:
        self.received = chunks
        return self.result


@pytest.fixture
def fake_extractor() -> FakeExtractor:
    return FakeExtractor(model_output_from_expected("current-health-policy"))
