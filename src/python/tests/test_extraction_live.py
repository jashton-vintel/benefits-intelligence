"""Runs real extraction against the sample policies. Opt in with: uv run pytest -m live"""

import json
from decimal import Decimal
from pathlib import Path
from typing import Any

import pytest
from pydantic import ValidationError

from app.config import Settings, load_settings
from app.services.document_preparation import prepare_document
from app.workers.policy_worker import create_extractor

SAMPLE_DATA = Path(__file__).resolve().parents[3] / "sample-data"
EXPECTED_FILES = sorted((SAMPLE_DATA / "expected").glob("*.json"))


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


async def test_configured_model_is_available() -> None:
    assert SETTINGS is not None

    await create_extractor(SETTINGS).verify()


@pytest.mark.parametrize("expected_file", EXPECTED_FILES, ids=lambda path: path.stem)
async def test_extracts_expected_header_fields(expected_file: Path) -> None:
    assert SETTINGS is not None
    expected: dict[str, Any] = json.loads(expected_file.read_text(encoding="utf-8"))
    facts = expected["facts"]
    prepared = prepare_document(SAMPLE_DATA / expected["document"])

    extraction = await create_extractor(SETTINGS).extract(prepared.chunks)

    # Compared together so a failure shows every mismatched field at once.
    actual = {
        "provider": extraction.provider,
        "scheme_name": extraction.scheme_name,
        "annual_excess": extraction.annual_excess,
    }
    wanted = {
        "provider": facts["provider"]["value"],
        "scheme_name": facts["scheme_name"]["value"],
        "annual_excess": Decimal(facts["annual_excess"]["value"]),
    }
    assert actual == wanted
