"""Runs real extraction against the sample policies. Opt in with: uv run pytest -m live"""

from contextlib import aclosing
from datetime import date
from decimal import Decimal
from typing import Any

import pytest
import pytest_asyncio
from conftest import SAMPLE_DATA, read_expected
from pydantic import ValidationError

from app.config import Settings, load_settings
from app.models.policy import Fact, FactIssue, PolicyExtraction
from app.services.document_preparation import prepare_document
from app.services.extraction_assembler import assemble_extraction
from app.workers.policy_worker import create_extractor

EXPECTED = sorted(path.stem for path in (SAMPLE_DATA / "expected").glob("*.json"))
EVIDENCE_PROBLEMS = {FactIssue.EVIDENCE_MISSING, FactIssue.EVIDENCE_NOT_FOUND}


def configured_settings() -> Settings | None:
    try:
        settings = load_settings()
    except ValidationError:
        return None
    key = settings.openai_api_key
    return settings if key and key.get_secret_value().strip() else None


SETTINGS = configured_settings()

pytestmark = [
    pytest.mark.live,
    pytest.mark.skipif(SETTINGS is None, reason="OPENAI_API_KEY is not configured"),
]


# One extraction per sample, shared by the tests below to keep live runs quick and cheap.
@pytest_asyncio.fixture(scope="module", loop_scope="module", params=EXPECTED)
async def extracted(request: pytest.FixtureRequest) -> tuple[PolicyExtraction, dict[str, Any]]:
    assert SETTINGS is not None
    expected = read_expected(request.param)
    prepared = prepare_document(SAMPLE_DATA / expected["document"])

    async with aclosing(create_extractor(SETTINGS)) as extractor:
        output = await extractor.extract(prepared.chunks)

    return assemble_extraction(output, prepared.document), expected


async def test_configured_model_is_available() -> None:
    assert SETTINGS is not None

    async with aclosing(create_extractor(SETTINGS)) as extractor:
        await extractor.verify()


def test_extracts_every_expected_value(
    extracted: tuple[PolicyExtraction, dict[str, Any]],
) -> None:
    extraction, expected = extracted
    facts = extraction.facts()

    # Collected together so a failure shows every mismatched fact at once.
    mismatches = {
        path: (facts[path].value, wanted["value"])
        for path, wanted in expected["facts"].items()
        if not wanted.get("ambiguous") and not matches(facts[path], wanted)
    }
    assert mismatches == {}


def test_ambiguous_facts_are_reported_rather_than_guessed(
    extracted: tuple[PolicyExtraction, dict[str, Any]],
) -> None:
    extraction, expected = extracted
    facts = extraction.facts()

    for path in (path for path, fact in expected["facts"].items() if fact.get("ambiguous")):
        assert facts[path].value is None, path
        assert FactIssue.AMBIGUOUS in facts[path].issues, path


def test_every_stated_value_has_evidence_found_in_the_document(
    extracted: tuple[PolicyExtraction, dict[str, Any]],
) -> None:
    extraction, _ = extracted

    unsupported = {
        path: [issue.value for issue in fact.issues if issue in EVIDENCE_PROBLEMS]
        for path, fact in extraction.facts().items()
        if fact.value is not None and EVIDENCE_PROBLEMS.intersection(fact.issues)
    }
    assert unsupported == {}


def matches(fact: Fact[Any], wanted: dict[str, Any]) -> bool:
    actual, value = fact.value, wanted["value"]
    if value is None or actual is None:
        return actual is None and value is None
    if keywords := wanted.get("keywords"):
        return all(keyword in str(actual).casefold() for keyword in keywords)
    if isinstance(actual, Decimal):
        return actual == Decimal(str(value))
    if isinstance(actual, date):
        return actual.isoformat() == value
    if isinstance(value, dict):
        # Limits are free text; only the parts with a definite answer are compared.
        return actual.covered == value["covered"] and actual.session_limit == value.get(
            "session_limit"
        )
    return actual == value
