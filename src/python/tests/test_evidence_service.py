from datetime import date
from decimal import Decimal
from pathlib import Path

import pytest

from app.models.document import PageContent
from app.services.document_preparation import prepare_document
from app.services.evidence_service import find_evidence, quote_states_amount, quote_states_date
from app.services.text_normaliser import NormalisedDocument, normalise_document

PROPOSED_POLICY = Path(__file__).resolve().parents[3] / "sample-data" / "ProposedHealthPolicy.pdf"


@pytest.fixture(scope="module")
def proposed_policy() -> NormalisedDocument:
    return prepare_document(PROPOSED_POLICY).document


def document(*pages: str) -> NormalisedDocument:
    return normalise_document(
        [PageContent(page_number=number, text=text) for number, text in enumerate(pages, 1)]
    )


def test_finds_the_page_a_quote_appears_on() -> None:
    evidence = find_evidence(
        document("Section 1. Who can join.", "An excess of £100 applies to each member."),
        "An excess of £100 applies",
    )

    assert evidence is not None
    assert (evidence.page_start, evidence.page_end) == (2, 2)
    assert evidence.quote == "An excess of £100 applies"


def test_quote_running_across_a_page_break_cites_both_pages() -> None:
    evidence = find_evidence(
        document("Cover is limited to eight", "sessions per member per year."),
        "limited to eight sessions per member",
    )

    assert evidence is not None
    assert (evidence.page_start, evidence.page_end) == (1, 2)


@pytest.mark.parametrize(
    "quote",
    [
        "osteopathy and chiropractic treatment are not covered under this scheme.",
        "Osteopathy and chiropractic treatment are not covered under this scheme",
        "Osteopathy  and chiropractic\ntreatment are not covered under this scheme",
    ],
    ids=["case-and-punctuation", "exact", "spacing"],
)
def test_tolerates_differences_that_do_not_change_the_wording(
    proposed_policy: NormalisedDocument, quote: str
) -> None:
    assert find_evidence(proposed_policy, quote) is not None


@pytest.mark.parametrize(
    "quote",
    [
        "Osteopathy and chiropractic treatment are covered in full under this scheme",
        "Physiotherapy is covered for up to ten sessions per person in each scheme year",
        "Physio is covered for a maximum of 10 sessions each year",
        "Every employee is eligible for this scheme with no excess applied to any claim",
    ],
    ids=["reversed-meaning", "word-dropped", "paraphrase", "fabricated"],
)
def test_rejects_quotes_the_document_does_not_contain(
    proposed_policy: NormalisedDocument, quote: str
) -> None:
    assert find_evidence(proposed_policy, quote) is None


@pytest.mark.parametrize("quote", ["", "   \n "])
def test_blank_quote_is_not_evidence(quote: str) -> None:
    assert find_evidence(document("Any text at all."), quote) is None


@pytest.mark.parametrize(
    ("amount", "quote", "expected"),
    [
        ("150", "An excess of £150 applies", True),
        ("120000", "The annual premium is £120,000", True),
        ("120000", "The annual premium is £120000.00", True),
        ("99.50", "a charge of £99.50 per visit", True),
        ("150", "up to £1,500 per member", False),
        ("500", "up to £1,500 per member", False),
        ("150", "a charge of £150.50", False),
        ("1500", "An excess of £150 applies", False),
    ],
)
def test_amount_must_be_stated_in_the_quote(amount: str, quote: str, expected: bool) -> None:
    assert quote_states_amount(Decimal(amount), quote) is expected


@pytest.mark.parametrize(
    ("value", "quote", "expected"),
    [
        (date(2026, 4, 1), "Cover starts on 1 April 2026", True),
        (date(2026, 4, 1), "effective from 1st Apr 2026", True),
        (date(2026, 4, 1), "effective from 01/04/2026", True),
        (date(2026, 4, 1), "effective from 2026-04-01", True),
        (date(2026, 4, 11), "Cover starts on 1 April 2026", False),
        (date(2025, 4, 1), "Cover starts on 1 April 2026", False),
        (date(1404, 1, 26), "Cover starts on 1 April 2026", False),
    ],
)
def test_date_must_be_stated_in_the_quote(value: date, quote: str, expected: bool) -> None:
    assert quote_states_date(value, quote) is expected
