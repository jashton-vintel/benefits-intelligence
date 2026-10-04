from decimal import Decimal
from pathlib import Path

import pytest

from app.models.comparison import ComparisonSummaryRequest, FieldDifference
from app.services.comparison_facts import describe_comparison, describe_difference, field_label

FIXTURE = (
    Path(__file__).resolve().parents[3]
    / "contracts"
    / "fixtures"
    / "comparison_summary_request.json"
)


def difference(**overrides: object) -> FieldDifference:
    values: dict[str, object] = {
        "field": "annual_premium",
        "kind": "money",
        "current": "120000.00",
        "proposed": "108000.00",
        "delta": Decimal("-12000.00"),
        "change": "decreased",
        "needs_review": False,
    }
    values.update(overrides)
    return FieldDifference.model_validate(values)


def test_reads_the_shared_contract_fixture() -> None:
    request = ComparisonSummaryRequest.model_validate_json(FIXTURE.read_text(encoding="utf-8"))

    assert request.current.provider == "Atlas Healthcare"
    assert request.differences[2].delta == Decimal("-12000.00")
    assert len(describe_comparison(request)) == len(request.differences)


def test_describes_a_change_in_money_with_its_size() -> None:
    assert describe_difference(difference()) == (
        "Annual premium: £120,000 → £108,000 (decreased by £12,000)."
    )


def test_describes_a_change_in_sessions() -> None:
    sessions = difference(
        field="coverage.physiotherapy.sessions",
        kind="count",
        current="8",
        proposed="10",
        delta=Decimal(2),
        change="increased",
    )

    assert (
        describe_difference(sessions) == "Physiotherapy sessions per year: 8 → 10 (increased by 2)."
    )


@pytest.mark.parametrize(
    ("overrides", "expected"),
    [
        (
            {
                "field": "dependants_allowed",
                "kind": "flag",
                "current": "true",
                "proposed": "true",
                "delta": None,
                "change": "unchanged",
            },
            "Dependants can join: yes in both.",
        ),
        (
            {
                "field": "effective_date",
                "kind": "date",
                "current": "2026-04-01",
                "proposed": "2027-04-01",
                "delta": None,
                "change": "changed",
            },
            "Effective date: 1 April 2026 → 1 April 2027 (changed).",
        ),
        (
            {
                "field": "eligibility.minimum_grade",
                "kind": "text",
                "current": "Grade 4",
                "proposed": None,
                "delta": None,
                "change": "removed",
            },
            'Minimum grade: "Grade 4" → no requirement (requirement removed).',
        ),
        (
            {
                "field": "annual_excess",
                "current": "100.50",
                "proposed": "150.00",
                "delta": Decimal("49.50"),
                "change": "increased",
            },
            "Annual excess: £100.50 → £150 (increased by £49.50).",
        ),
    ],
    ids=["unchanged-flag", "changed-date", "removed-requirement", "pence"],
)
def test_describes_each_kind_of_change(overrides: dict[str, object], expected: str) -> None:
    assert describe_difference(difference(**overrides)) == expected


def test_unconfirmed_difference_is_described_as_such() -> None:
    unknown = difference(
        field="dependants_included",
        kind="flag",
        current="true",
        proposed=None,
        delta=None,
        change="unknown",
        needs_review=True,
    )

    assert describe_difference(unknown) == (
        "Dependant cover paid by the employer: yes → not stated (cannot be compared)."
        " Not yet confirmed: needs review."
    )


@pytest.mark.parametrize(
    ("path", "label"),
    [
        ("annual_excess", "Annual excess"),
        ("coverage.mental_health.covered", "Mental health covered"),
        ("coverage.cancer.limit", "Cancer care terms"),
        ("eligibility.country", "Eligible country of residence"),
        ("coverage.dental.covered", "Dental covered"),
        ("something_new", "Something new"),
    ],
)
def test_labels_fields_including_ones_it_does_not_know(path: str, label: str) -> None:
    assert field_label(path) == label
