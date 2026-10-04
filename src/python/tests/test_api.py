import json
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from app.api.security import INTERNAL_KEY_HEADER
from app.config import ApiSettings
from app.main import build_app, create_app
from app.models.comparison import ComparisonSummaryRequest
from app.models.question import Citation, QuestionAnswer, QuestionRequest
from app.services.comparison_summariser import SummaryUnavailableError
from app.services.question_answerer import AnswerUnavailableError, refusal

FIXTURE = (
    Path(__file__).resolve().parents[3]
    / "contracts"
    / "fixtures"
    / "comparison_summary_request.json"
)
KEY = "k" * 32


class FakeSummariser:
    def __init__(self, result: str | Exception = "The annual premium falls by £12,000.") -> None:
        self.result = result
        self.received: list[ComparisonSummaryRequest] = []

    async def summarise(self, request: ComparisonSummaryRequest) -> str:
        self.received.append(request)
        if isinstance(self.result, Exception):
            raise self.result
        return self.result


class FakeAnswerer:
    def __init__(self, result: QuestionAnswer | Exception | None = None) -> None:
        self.result = result or QuestionAnswer(
            answer="Yes, up to ten sessions each scheme year.",
            supported=True,
            citations=[Citation(page_start=4, page_end=4, quote="up to ten sessions")],
        )
        self.received: list[QuestionRequest] = []

    async def answer(self, request: QuestionRequest) -> QuestionAnswer:
        self.received.append(request)
        if isinstance(self.result, Exception):
            raise self.result
        return self.result


def client(
    summariser: FakeSummariser | None = None, answerer: FakeAnswerer | None = None
) -> TestClient:
    return TestClient(create_app(summariser or FakeSummariser(), answerer or FakeAnswerer(), KEY))


def question() -> dict[str, object]:
    return {
        "question": "Is physiotherapy covered?",
        "policy_name": "Proposed policy",
        "pages": [{"page_number": 1, "text": "Physiotherapy is covered for up to ten sessions."}],
    }


def comparison() -> dict[str, object]:
    return json.loads(FIXTURE.read_text(encoding="utf-8"))


def test_health_needs_no_key() -> None:
    response = client().get("/health")

    assert response.status_code == 200
    assert response.json() == {"status": "healthy"}


def test_returns_the_comparison_summary() -> None:
    summariser = FakeSummariser()

    response = client(summariser).post(
        "/summaries/comparison", json=comparison(), headers={INTERNAL_KEY_HEADER: KEY}
    )

    assert response.status_code == 200
    assert response.json() == {"summary": "The annual premium falls by £12,000."}
    assert summariser.received[0].proposed.provider == "NorthStar Health"


@pytest.mark.parametrize("headers", [{}, {INTERNAL_KEY_HEADER: "wrong"}], ids=["missing", "wrong"])
def test_summary_requires_the_internal_key(headers: dict[str, str]) -> None:
    summariser = FakeSummariser()

    response = client(summariser).post("/summaries/comparison", json=comparison(), headers=headers)

    assert response.status_code == 401
    assert summariser.received == []


def test_invalid_comparison_is_rejected_before_summarising() -> None:
    summariser = FakeSummariser()
    body = comparison() | {"differences": []}

    response = client(summariser).post(
        "/summaries/comparison", json=body, headers={INTERNAL_KEY_HEADER: KEY}
    )

    assert response.status_code == 422
    assert summariser.received == []


def test_unavailable_summary_is_reported_as_service_unavailable() -> None:
    summariser = FakeSummariser(SummaryUnavailableError("The model returned an empty summary."))

    response = client(summariser).post(
        "/summaries/comparison", json=comparison(), headers={INTERNAL_KEY_HEADER: KEY}
    )

    assert response.status_code == 503
    assert response.json() == {"detail": "The model returned an empty summary."}


@pytest.mark.parametrize("key", [None, "", "too-short"], ids=["unset", "empty", "short"])
def test_api_refuses_to_start_without_a_strong_internal_key(key: str | None) -> None:
    settings = ApiSettings.model_validate({"internal_api_key": key, "openai_api_key": "sk-test"})

    with pytest.raises(RuntimeError, match="INTERNAL_API_KEY"):
        build_app(settings)


def test_answers_a_question_with_its_citations() -> None:
    answerer = FakeAnswerer()

    response = client(answerer=answerer).post(
        "/answers", json=question(), headers={INTERNAL_KEY_HEADER: KEY}
    )

    assert response.status_code == 200
    assert response.json() == {
        "answer": "Yes, up to ten sessions each scheme year.",
        "supported": True,
        "citations": [{"page_start": 4, "page_end": 4, "quote": "up to ten sessions"}],
    }
    assert answerer.received[0].pages[0].page_number == 1


def test_question_the_policy_cannot_answer_is_refused_not_failed() -> None:
    response = client(answerer=FakeAnswerer(refusal())).post(
        "/answers", json=question(), headers={INTERNAL_KEY_HEADER: KEY}
    )

    assert response.status_code == 200
    assert response.json()["supported"] is False


def test_answers_require_the_internal_key() -> None:
    answerer = FakeAnswerer()

    response = client(answerer=answerer).post("/answers", json=question())

    assert response.status_code == 401
    assert answerer.received == []


@pytest.mark.parametrize(
    "change",
    [{"question": ""}, {"question": "x" * 501}, {"pages": []}],
    ids=["empty-question", "long-question", "no-pages"],
)
def test_invalid_questions_are_rejected(change: dict[str, object]) -> None:
    response = client().post(
        "/answers", json=question() | change, headers={INTERNAL_KEY_HEADER: KEY}
    )

    assert response.status_code == 422


def test_unavailable_answer_is_reported_as_service_unavailable() -> None:
    answerer = FakeAnswerer(AnswerUnavailableError("The answer service request failed."))

    response = client(answerer=answerer).post(
        "/answers", json=question(), headers={INTERNAL_KEY_HEADER: KEY}
    )

    assert response.status_code == 503
