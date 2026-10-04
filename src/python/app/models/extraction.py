from datetime import date

from pydantic import BaseModel, Field


class ExtractedFact[T](BaseModel):
    """One value as reported by the model, with the passage it was read from.

    These models are the structure requested from the model, so they stay simple (plain
    numbers, explicit nulls) and carry the prompt's field guidance. They are converted into
    PolicyExtraction before anything leaves the worker, so the prompt can evolve without
    changing the contract with the API.
    """

    value: T | None
    quote: str | None = Field(
        description=(
            "The shortest passage that states the value, copied exactly from the document. "
            "Null if the document does not address this field."
        )
    )
    confidence: float = Field(
        description=(
            "How certain you are that the value is correct and complete, from 0 to 1. "
            "Use a low score when the value had to be interpreted rather than read directly."
        )
    )
    ambiguous: bool = Field(
        description=(
            "True when the document leaves the value open, states conflicting values, or "
            "makes it depend on conditions it does not settle."
        )
    )


class ExtractedCoverageTerms(BaseModel):
    covered: bool
    limit: str | None = Field(
        description=(
            "A short summary of the limits and key conditions on this cover, using the "
            "document's own figures, for example '£1,000 per member per policy year' or "
            "'Paid in full at network hospitals'. Null if none are stated."
        )
    )
    session_limit: int | None = Field(
        description=(
            "Maximum number of treatment sessions or visits per year, if the cover is limited "
            "that way. Limits expressed in days, nights or money are not session limits."
        )
    )


class ExtractedCoverage(BaseModel):
    inpatient: ExtractedFact[ExtractedCoverageTerms] = Field(
        description="In-patient and day-patient hospital treatment."
    )
    outpatient: ExtractedFact[ExtractedCoverageTerms] = Field(
        description="Out-patient specialist consultations."
    )
    diagnostics: ExtractedFact[ExtractedCoverageTerms] = Field(
        description="Diagnostic tests and scans."
    )
    physiotherapy: ExtractedFact[ExtractedCoverageTerms] = Field(
        description="Physiotherapy, including any limit it shares with other therapies."
    )
    mental_health: ExtractedFact[ExtractedCoverageTerms] = Field(
        description="Mental health treatment, out-patient and in-patient."
    )
    cancer: ExtractedFact[ExtractedCoverageTerms] = Field(description="Cancer treatment.")


class ExtractedEligibility(BaseModel):
    employment_type: ExtractedFact[str] = Field(
        description=(
            "The contract types that make an employee eligible, as stated, for example "
            "'Permanent'. Follow any definition of 'employee' the document gives."
        )
    )
    country: ExtractedFact[str] = Field(
        description="Where employees must live or work to be eligible."
    )
    minimum_service_months: ExtractedFact[int] = Field(
        description=(
            "Months of service required before joining. 0 if the document says there is no minimum."
        )
    )
    minimum_grade: ExtractedFact[str] = Field(
        description=(
            "The lowest grade or level that is eligible. Null value, with the passage as the "
            "quote, if the document says cover is not restricted by grade."
        )
    )


class ExtractedPolicy(BaseModel):
    provider: ExtractedFact[str] = Field(description="Name of the insurer or health provider.")
    scheme_name: ExtractedFact[str] = Field(
        description=(
            "The scheme's or plan's own product name only. Exclude descriptions of the type of "
            "cover (for example 'private medical insurance' or 'healthcare scheme') and words "
            "describing the document itself (for example 'member guide' or 'summary')."
        )
    )
    annual_premium: ExtractedFact[float] = Field(
        description="Total annual premium for the scheme, in pounds, as a number."
    )
    annual_excess: ExtractedFact[float] = Field(
        description="The excess each member pays per policy year, in pounds, as a number."
    )
    effective_date: ExtractedFact[date] = Field(
        description="The date cover under this policy starts, as a Gregorian calendar date."
    )
    renewal_date: ExtractedFact[date] = Field(
        description="The next renewal date, as a Gregorian calendar date."
    )
    dependants_allowed: ExtractedFact[bool] = Field(
        description="Whether employees can add dependants such as a partner or children."
    )
    dependants_included: ExtractedFact[bool] = Field(
        description=(
            "Whether the employer pays for dependants' cover, so it costs the employee nothing."
        )
    )
    coverage: ExtractedCoverage
    eligibility: ExtractedEligibility
