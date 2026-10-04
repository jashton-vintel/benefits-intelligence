import re
from decimal import Decimal
from typing import Self

from pydantic import BaseModel, ConfigDict, Field

_PENNY = Decimal("0.01")
_LEGAL_SUFFIX = re.compile(r"[\s,]+(limited|ltd|plc|llp|inc)\.?$", re.IGNORECASE)


class ExtractedPolicyHeader(BaseModel):
    """The structure requested from the model.

    Kept deliberately simple (plain numbers, explicit nulls) because it is what the model
    sees. It is converted into PolicyExtraction before anything leaves the worker, so the
    prompt format can evolve without changing the contract with the API.
    """

    provider: str | None = Field(
        description=(
            "Name of the insurer or health provider. Null if the document does not state it."
        )
    )
    scheme_name: str | None = Field(
        description=(
            "The scheme's or plan's own product name only. Exclude descriptions of the type of "
            "cover (for example 'private medical insurance' or 'healthcare scheme') and words "
            "describing the document itself (for example 'member guide' or 'summary'). "
            "Null if the document does not state it."
        )
    )
    annual_excess: float | None = Field(
        description=(
            "The excess each member pays per policy year, in pounds, as a number. "
            "Null if the document does not state an excess."
        )
    )


class PolicyExtraction(BaseModel):
    model_config = ConfigDict(frozen=True)

    # Lengths match what the API stores, so oversized values fail extraction here rather
    # than failing later when the result is saved.
    provider: str | None = Field(max_length=200)
    scheme_name: str | None = Field(max_length=200)
    annual_excess: Decimal | None = Field(ge=0)

    @classmethod
    def from_model_output(cls, output: ExtractedPolicyHeader) -> Self:
        excess = output.annual_excess
        return cls(
            provider=_without_legal_suffix(_clean(output.provider)),
            scheme_name=_clean(output.scheme_name),
            annual_excess=None if excess is None else Decimal(str(excess)).quantize(_PENNY),
        )


class DocumentSummary(BaseModel):
    model_config = ConfigDict(frozen=True)

    page_count: int = Field(ge=1)
    chunk_count: int = Field(ge=1)


def _clean(value: str | None) -> str | None:
    if value is None:
        return None
    return value.strip() or None


def _without_legal_suffix(name: str | None) -> str | None:
    # Deterministic formatting stays in code; the model is only asked to find the name.
    if name is None:
        return None
    return _LEGAL_SUFFIX.sub("", name).strip() or name
