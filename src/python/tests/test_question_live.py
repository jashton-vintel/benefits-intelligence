"""Answers the sample questions with the real model. Opt in with: uv run pytest -m live"""

from contextlib import aclosing
from typing import Any

import pytest
from conftest import SAMPLE_DATA, read_expected
from pydantic import ValidationError

from app.config import ApiSettings, load_api_settings
from app.models.question import QuestionRequest
from app.services.openai_client import create_openai_client
from app.services.pdf_parser import parse_pdf
from app.services.question_answerer import REFUSAL, OpenAIQuestionAnswerer
from app.services.text_normaliser import flatten, normalise_text


def configured_settings() -> ApiSettings | None:
    try:
        settings = load_api_settings()
    except ValidationError:
        return None
    key = settings.openai_api_key
    return settings if key and key.get_secret_value().strip() else None


def sample_questions() -> list[Any]:
    cases = []
    for name in ("current-health-policy", "proposed-health-policy"):
        expected = read_expected(name)
        for question in expected["questions"]:
            cases.append(pytest.param(expected["document"], question, id=question["question"]))
    return cases


SETTINGS = configured_settings()

pytestmark = [
    pytest.mark.live,
    pytest.mark.skipif(SETTINGS is None, reason="OPENAI_API_KEY is not configured"),
]


@pytest.mark.parametrize(("document", "question"), sample_questions())
async def test_answers_sample_questions_from_the_policy_alone(
    document: str, question: dict[str, Any]
) -> None:
    assert SETTINGS is not None
    request = QuestionRequest(
        question=question["question"],
        policy_name=document,
        pages=parse_pdf(SAMPLE_DATA / document),
    )
    answerer = OpenAIQuestionAnswerer(create_openai_client(SETTINGS), SETTINGS.openai_model)

    async with aclosing(answerer):
        answer = await answerer.answer(request)

    print(f"\n{question['question']}\n  {answer.answer}")
    for citation in answer.citations:
        print(f"  p.{citation.page_start}: {citation.quote}")

    if question["answerable"]:
        assert answer.supported
        quotes = " ".join(flatten(normalise_text(c.quote)).lower() for c in answer.citations)
        expected_evidence = flatten(normalise_text(question["evidence"])).lower()
        assert expected_evidence[:40] in quotes, "expected passage was not cited"
    else:
        assert (answer.answer, answer.supported, answer.citations) == (REFUSAL, False, [])
