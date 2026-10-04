"""Keeps the synthetic sample policies and their expected answers consistent.

Every evidence quote in sample-data/expected must be findable in the rendered PDF, so later
extraction and evaluation tests can rely on it as ground truth.
"""

import json
from pathlib import Path
from typing import Any

import pytest

from app.services.document_chunker import chunk_document
from app.services.pdf_parser import parse_pdf
from app.services.text_normaliser import NormalisedDocument, flatten, normalise_document

SAMPLE_DATA = Path(__file__).resolve().parents[3] / "sample-data"
EXPECTED_FILES = sorted((SAMPLE_DATA / "expected").glob("*.json"))


def load(expected_file: Path) -> tuple[dict[str, Any], NormalisedDocument]:
    expected = json.loads(expected_file.read_text(encoding="utf-8"))
    document = normalise_document(parse_pdf(SAMPLE_DATA / expected["document"]))
    return expected, document


def evidence_quotes(expected: dict[str, Any]) -> list[str]:
    quotes = [fact["evidence"] for fact in expected["facts"].values()]
    quotes += [check["evidence"] for check in expected["checks"]]
    quotes += [question["evidence"] for question in expected["questions"] if "evidence" in question]
    return quotes


@pytest.mark.parametrize("expected_file", EXPECTED_FILES, ids=lambda path: path.stem)
def test_every_expected_evidence_quote_is_in_the_document(expected_file: Path) -> None:
    expected, document = load(expected_file)

    missing = [quote for quote in evidence_quotes(expected) if document.locate(quote) is None]

    assert missing == []


@pytest.mark.parametrize("expected_file", EXPECTED_FILES, ids=lambda path: path.stem)
def test_unanswerable_questions_have_no_supporting_text(expected_file: Path) -> None:
    expected, document = load(expected_file)

    for question in expected["questions"]:
        if not question["answerable"]:
            present = [
                term for term in question["absent_terms"] if term.lower() in document.text.lower()
            ]
            assert present == [], question["question"]


@pytest.mark.parametrize("expected_file", EXPECTED_FILES, ids=lambda path: path.stem)
def test_every_evidence_quote_falls_within_a_single_chunk(expected_file: Path) -> None:
    expected, document = load(expected_file)
    chunks = chunk_document(document)

    for quote in evidence_quotes(expected):
        assert any(flatten(quote) in flatten(chunk.text) for chunk in chunks), quote


def test_dependant_cost_clause_is_recovered_across_the_page_break() -> None:
    _, document = load(SAMPLE_DATA / "expected" / "proposed-health-policy.json")

    location = document.locate(
        "Arrangements for the cost of dependant cover, including whether any contribution "
        "will be required from the employee, will be confirmed at renewal"
    )

    assert location is not None
    assert location.page_end == location.page_start + 1
