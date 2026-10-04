from decimal import Decimal

import pytest
from pydantic import ValidationError

from app.models.extraction import ExtractedPolicyHeader, PolicyExtraction


def output(**overrides: object) -> ExtractedPolicyHeader:
    values: dict[str, object] = {
        "provider": "Atlas Healthcare",
        "scheme_name": "Corporate Plus",
        "annual_excess": 100,
    }
    values.update(overrides)
    return ExtractedPolicyHeader.model_validate(values)


def test_converts_excess_to_pounds_and_pence() -> None:
    extraction = PolicyExtraction.from_model_output(output(annual_excess=149.999))

    assert extraction.annual_excess == Decimal("150.00")


def test_keeps_missing_values_as_null() -> None:
    extraction = PolicyExtraction.from_model_output(
        output(provider=None, scheme_name=None, annual_excess=None)
    )

    assert extraction == PolicyExtraction(provider=None, scheme_name=None, annual_excess=None)


def test_treats_blank_text_as_missing() -> None:
    extraction = PolicyExtraction.from_model_output(output(provider="  ", scheme_name=" Plus "))

    assert extraction.provider is None
    assert extraction.scheme_name == "Plus"


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
    ],
)
def test_removes_legal_suffix_from_provider(raw: str, expected: str) -> None:
    assert PolicyExtraction.from_model_output(output(provider=raw)).provider == expected


def test_rejects_negative_excess() -> None:
    with pytest.raises(ValidationError):
        PolicyExtraction.from_model_output(output(annual_excess=-10))


def test_rejects_values_longer_than_the_api_stores() -> None:
    with pytest.raises(ValidationError):
        PolicyExtraction.from_model_output(output(provider="x" * 201))


def test_serialises_excess_as_exact_decimal_string() -> None:
    extraction = PolicyExtraction.from_model_output(output(annual_excess=150))

    assert extraction.model_dump(mode="json")["annual_excess"] == "150.00"
