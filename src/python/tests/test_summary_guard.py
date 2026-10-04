import pytest

from app.services.summary_guard import judgement_words, unsupported_numbers

FACTS = [
    "- Annual premium: £120,000 → £108,000 (decreased by £12,000).",
    "- Physiotherapy sessions per year: 8 → 10 (increased by 2).",
    "- Effective date: 1 April 2026 → 1 April 2027 (changed).",
]


@pytest.mark.parametrize(
    ("summary", "words"),
    [
        ("The proposed policy is better value.", ["better"]),
        ("We recommend the proposed scheme; employers should switch.", ["recommend", "should"]),
        ("This is the best option and offers good value.", ["best", "good value"]),
        ("The annual premium decreases by £12,000.", []),
        ("Physiotherapy cover rises from 8 to 10 sessions.", []),
    ],
)
def test_finds_words_that_judge_or_advise(summary: str, words: list[str]) -> None:
    assert judgement_words(summary) == words


def test_accepts_figures_taken_from_the_facts() -> None:
    summary = (
        "The annual premium falls from £120,000 to £108,000, a reduction of 12000, and "
        "physiotherapy rises from 8 to 10 sessions from 1 April 2027."
    )

    assert unsupported_numbers(summary, FACTS) == []


def test_reports_figures_that_are_not_in_the_facts() -> None:
    summary = "The premium falls by £15,000 and physiotherapy rises to 12 sessions."

    assert unsupported_numbers(summary, FACTS) == ["12", "15000"]
