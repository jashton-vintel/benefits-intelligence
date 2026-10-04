from decimal import Decimal
from typing import Literal
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field

ValueKind = Literal["text", "money", "date", "flag", "count"]
Change = Literal["unchanged", "increased", "decreased", "changed", "added", "removed", "unknown"]


class PolicyReference(BaseModel):
    model_config = ConfigDict(frozen=True)

    id: UUID
    name: str = Field(max_length=200)
    provider: str | None = Field(default=None, max_length=200)
    scheme_name: str | None = Field(default=None, max_length=200)


class FieldDifference(BaseModel):
    model_config = ConfigDict(frozen=True)

    field: str = Field(max_length=100)
    kind: ValueKind
    current: str | None = Field(max_length=2000)
    proposed: str | None = Field(max_length=2000)
    delta: Decimal | None
    change: Change
    needs_review: bool


class ComparisonSummaryRequest(BaseModel):
    """A comparison already calculated by the API; the summary is written from it alone."""

    model_config = ConfigDict(frozen=True)

    current: PolicyReference
    proposed: PolicyReference
    differences: list[FieldDifference] = Field(min_length=1, max_length=200)


class ComparisonSummaryResponse(BaseModel):
    model_config = ConfigDict(frozen=True)

    summary: str
