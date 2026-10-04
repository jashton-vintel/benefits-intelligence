from datetime import date
from decimal import Decimal
from typing import Any

import pytest
from conftest import SAMPLE_DATA, model_output_from_expected, read_expected

from app.models.extraction import ExtractedPolicy
from app.models.policy import FactIssue, PolicyExtraction
from app.services.document_preparation import prepare_document
from app.services.errors import ExtractionFailedError
from app.services.extraction_assembler import assemble_extraction
from app.services.text_normaliser import NormalisedDocument

FABRICATED = "Physiotherapy is covered in full with no limit on the number of sessions"


@pytest.fixture(scope="module")
def current_policy() -> NormalisedDocument:
    return prepare_document(SAMPLE_DATA / "CurrentHealthPolicy.pdf").document


@pytest.fixture(scope="module")
def proposed_policy() -> NormalisedDocument:
    return prepare_document(SAMPLE_DATA / "ProposedHealthPolicy.pdf").document


def output_with(path: str, **changes: Any) -> ExtractedPolicy:
    """The current policy's correct answer with one fact changed."""
    output = model_output_from_expected("current-health-policy").model_dump()
    group, _, field = path.rpartition(".")
    (output[group] if group else output)[field].update(changes)
    return ExtractedPolicy.model_validate(output)


def assemble(output: ExtractedPolicy, document: NormalisedDocument) -> PolicyExtraction:
    return assemble_extraction(output, document)


def test_converts_the_correct_answer_with_evidence_for_every_fact(
    current_policy: NormalisedDocument,
) -> None:
    extraction = assemble(model_output_from_expected("current-health-policy"), current_policy)

    assert extraction.provider.value == "Atlas Healthcare"
    assert extraction.annual_premium.value == Decimal("120000.00")
    assert extraction.effective_date.value == date(2026, 4, 1)
    assert extraction.coverage.physiotherapy.value is not None
    assert extraction.coverage.physiotherapy.value.session_limit == 8
    assert extraction.eligibility.minimum_service_months.value == 3
    assert {path: fact.issues for path, fact in extraction.facts().items() if fact.issues} == {}
    assert all(fact.evidence is not None for fact in extraction.facts().values())


def test_ambiguous_fact_is_reported_with_its_evidence(proposed_policy: NormalisedDocument) -> None:
    extraction = assemble(model_output_from_expected("proposed-health-policy"), proposed_policy)

    dependants_included = extraction.dependants_included
    assert dependants_included.value is None
    assert dependants_included.issues == (FactIssue.AMBIGUOUS,)
    assert dependants_included.evidence is not None
    assert dependants_included.evidence.page_start >= 1


def test_value_stated_as_absent_keeps_its_evidence(proposed_policy: NormalisedDocument) -> None:
    extraction = assemble(model_output_from_expected("proposed-health-policy"), proposed_policy)

    grade = extraction.eligibility.minimum_grade
    assert grade.value is None
    assert grade.evidence is not None
    assert grade.issues == ()


def test_fabricated_quote_is_reported_and_given_no_evidence(
    current_policy: NormalisedDocument,
) -> None:
    output = output_with("coverage.physiotherapy", quote=FABRICATED)

    physiotherapy = assemble(output, current_policy).coverage.physiotherapy

    assert physiotherapy.evidence is None
    assert physiotherapy.issues == (FactIssue.EVIDENCE_NOT_FOUND,)


def test_value_without_a_quote_is_reported(current_policy: NormalisedDocument) -> None:
    excess = assemble(output_with("annual_excess", quote=None), current_policy).annual_excess

    assert excess.value == Decimal("100.00")
    assert excess.issues == (FactIssue.EVIDENCE_MISSING,)


@pytest.mark.parametrize(
    ("path", "value"),
    [("annual_excess", 1000), ("annual_premium", 12000), ("effective_date", "1404-01-26")],
)
def test_value_the_quote_does_not_state_is_reported(
    current_policy: NormalisedDocument, path: str, value: object
) -> None:
    fact = assemble(output_with(path, value=value), current_policy).facts()[path]

    assert fact.evidence is not None
    assert fact.issues == (FactIssue.VALUE_NOT_IN_EVIDENCE,)


def test_field_the_document_does_not_address_has_no_issues(
    current_policy: NormalisedDocument,
) -> None:
    output = output_with("renewal_date", value=None, quote=None)

    renewal = assemble(output, current_policy).renewal_date

    assert (renewal.value, renewal.evidence, renewal.issues) == (None, None, ())


def test_every_problem_with_a_fact_is_reported(current_policy: NormalisedDocument) -> None:
    output = output_with("annual_excess", quote=FABRICATED, ambiguous=True)

    excess = assemble(output, current_policy).annual_excess

    assert excess.issues == (FactIssue.EVIDENCE_NOT_FOUND, FactIssue.AMBIGUOUS)


@pytest.mark.parametrize(("reported", "expected"), [(1.4, 1.0), (-0.2, 0.0), (0.73, 0.73)])
def test_confidence_is_kept_within_range(
    current_policy: NormalisedDocument, reported: float, expected: float
) -> None:
    output = output_with("provider", confidence=reported)

    assert assemble(output, current_policy).provider.confidence == expected


@pytest.mark.parametrize(
    ("raw", "expected"),
    [
        ("NorthStar Health plc", "NorthStar Health"),
        ("Atlas Healthcare Limited", "Atlas Healthcare"),
        ("Summit Medical Ltd.", "Summit Medical"),
        ("Acme Health, LLP", "Acme Health"),
        ("Atlas Healthcare", "Atlas Healthcare"),
        ("Limited Edition Health", "Limited Edition Health"),
        ("Plc", "Plc"),
        ("   ", None),
    ],
)
def test_provider_is_reported_without_a_legal_suffix(
    current_policy: NormalisedDocument, raw: str, expected: str | None
) -> None:
    output = output_with("provider", value=raw)

    assert assemble(output, current_policy).provider.value == expected


def test_money_is_rounded_to_pence_and_sent_as_an_exact_string(
    current_policy: NormalisedDocument,
) -> None:
    extraction = assemble(output_with("annual_excess", value=149.999), current_policy)

    assert extraction.annual_excess.value == Decimal("150.00")
    assert extraction.model_dump(mode="json")["annual_excess"]["value"] == "150.00"


@pytest.mark.parametrize(
    ("path", "value"),
    [
        ("annual_excess", -10),
        ("provider", "x" * 201),
        ("eligibility.minimum_service_months", -1),
        ("coverage.cancer", {"covered": True, "limit": "x" * 501, "session_limit": None}),
    ],
    ids=["negative-money", "long-name", "negative-count", "long-limit"],
)
def test_values_outside_what_the_api_accepts_fail_extraction(
    current_policy: NormalisedDocument, path: str, value: object
) -> None:
    with pytest.raises(ExtractionFailedError, match="failed validation"):
        assemble(output_with(path, value=value), current_policy)


@pytest.mark.parametrize(
    ("path", "changes"),
    [
        ("annual_premium", {"quote": "The annual premium is \u00163,000."}),
        ("coverage.outpatient", {"value": {"covered": True, "limit": "Up to \x00A33,000"}}),
    ],
    ids=["quote", "limit"],
)
def test_corrupted_model_text_fails_extraction(
    current_policy: NormalisedDocument, path: str, changes: dict[str, Any]
) -> None:
    if "value" in changes:
        changes["value"] |= {"session_limit": None}

    with pytest.raises(ExtractionFailedError, match="corrupted"):
        assemble(output_with(path, **changes), current_policy)


def test_extraction_reports_every_fact_in_the_schema(current_policy: NormalisedDocument) -> None:
    extraction = assemble(model_output_from_expected("current-health-policy"), current_policy)

    assert set(extraction.facts()) == set(read_expected("current-health-policy")["facts"])
