"""Summarises the sample comparison with the real model. Opt in with: uv run pytest -m live"""

from contextlib import aclosing
from pathlib import Path

import pytest
from pydantic import ValidationError

from app.config import ApiSettings, load_api_settings
from app.models.comparison import ComparisonSummaryRequest
from app.services.comparison_summariser import (
    OpenAIComparisonSummariser,
    comparison_lines,
    summary_problems,
)
from app.services.openai_client import create_openai_client

FIXTURE = (
    Path(__file__).resolve().parents[3]
    / "contracts"
    / "fixtures"
    / "comparison_summary_request.json"
)


def configured_settings() -> ApiSettings | None:
    try:
        settings = load_api_settings()
    except ValidationError:
        return None
    key = settings.openai_api_key
    return settings if key and key.get_secret_value().strip() else None


SETTINGS = configured_settings()

pytestmark = [
    pytest.mark.live,
    pytest.mark.skipif(SETTINGS is None, reason="OPENAI_API_KEY is not configured"),
]


async def test_summary_states_the_differences_without_judging_them() -> None:
    assert SETTINGS is not None
    request = ComparisonSummaryRequest.model_validate_json(FIXTURE.read_text(encoding="utf-8"))
    summariser = OpenAIComparisonSummariser(create_openai_client(SETTINGS), SETTINGS.openai_model)

    async with aclosing(summariser):
        summary = await summariser.summarise(request)

    print(summary)
    assert summary_problems(summary, comparison_lines(request)) == []
    assert "108,000" in summary
    assert "review" in summary.lower() or "confirmed" in summary.lower()
