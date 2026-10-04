from datetime import date
from decimal import Decimal
from enum import StrEnum
from typing import Annotated, Any, Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator

# Limits match what the API stores, so oversized values fail extraction here rather than
# failing later when the result is saved.
Name = Annotated[str, Field(min_length=1, max_length=200)]
Description = Annotated[str, Field(min_length=1, max_length=500)]
Money = Annotated[Decimal, Field(ge=0, max_digits=18, decimal_places=2)]
Count = Annotated[int, Field(ge=0, le=1200)]


class FactIssue(StrEnum):
    AMBIGUOUS = "ambiguous"
    EVIDENCE_MISSING = "evidence_missing"
    EVIDENCE_NOT_FOUND = "evidence_not_found"
    VALUE_NOT_IN_EVIDENCE = "value_not_in_evidence"


class Evidence(BaseModel):
    model_config = ConfigDict(frozen=True)

    page_start: int = Field(ge=1)
    page_end: int = Field(ge=1)
    quote: str = Field(min_length=1, max_length=2000)

    @model_validator(mode="after")
    def _pages_in_order(self) -> "Evidence":
        if self.page_end < self.page_start:
            raise ValueError("page_end must not be before page_start")
        return self


class Fact[T](BaseModel):
    """An extracted value with what is known about how reliable it is.

    The worker reports confidence and any issues it found; deciding what needs a person to
    review it is left to the API, so that policy can change without re-extracting.
    """

    model_config = ConfigDict(frozen=True)

    value: T | None
    confidence: float = Field(ge=0, le=1)
    evidence: Evidence | None
    issues: tuple[FactIssue, ...] = ()


class CoverageTerms(BaseModel):
    model_config = ConfigDict(frozen=True)

    covered: bool
    limit: Description | None
    session_limit: Count | None


class Coverage(BaseModel):
    model_config = ConfigDict(frozen=True)

    inpatient: Fact[CoverageTerms]
    outpatient: Fact[CoverageTerms]
    diagnostics: Fact[CoverageTerms]
    physiotherapy: Fact[CoverageTerms]
    mental_health: Fact[CoverageTerms]
    cancer: Fact[CoverageTerms]


class Eligibility(BaseModel):
    model_config = ConfigDict(frozen=True)

    employment_type: Fact[Description]
    country: Fact[Description]
    minimum_service_months: Fact[Count]
    minimum_grade: Fact[Name]


class PolicyExtraction(BaseModel):
    model_config = ConfigDict(frozen=True)

    schema_version: Literal["1.0"] = "1.0"
    benefit_type: Literal["private_medical"] = "private_medical"
    provider: Fact[Name]
    scheme_name: Fact[Name]
    annual_premium: Fact[Money]
    annual_excess: Fact[Money]
    effective_date: Fact[date]
    renewal_date: Fact[date]
    dependants_allowed: Fact[bool]
    dependants_included: Fact[bool]
    coverage: Coverage
    eligibility: Eligibility

    def facts(self) -> dict[str, Fact[Any]]:
        """Every fact keyed by its path, for example 'coverage.physiotherapy'."""
        header = {name: value for name, value in self if isinstance(value, Fact)}
        coverage = {f"coverage.{name}": value for name, value in self.coverage}
        eligibility = {f"eligibility.{name}": value for name, value in self.eligibility}
        return header | coverage | eligibility
