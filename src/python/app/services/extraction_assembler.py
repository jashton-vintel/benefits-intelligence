import re
from collections.abc import Callable
from decimal import Decimal

from pydantic import ValidationError

from app.models.extraction import ExtractedCoverageTerms, ExtractedFact, ExtractedPolicy
from app.models.policy import (
    Coverage,
    CoverageTerms,
    Eligibility,
    Fact,
    FactIssue,
    PolicyExtraction,
)
from app.services.errors import ExtractionFailedError
from app.services.evidence_service import find_evidence, quote_states_amount, quote_states_date
from app.services.text_normaliser import NormalisedDocument

_PENNY = Decimal("0.01")
_LEGAL_SUFFIX = re.compile(r"[\s,]+(limited|ltd|plc|llp|inc)\.?$", re.IGNORECASE)
_CONTROL_CHARACTERS = re.compile(r"[\x00-\x08\x0b\x0c\x0e-\x1f\x7f]")


def assemble_extraction(output: ExtractedPolicy, document: NormalisedDocument) -> PolicyExtraction:
    """Turn the model's answer into the published extraction.

    Every quote is checked against the document, values are converted to their exact types,
    and deterministic formatting is applied here rather than left to the prompt.
    """

    def fact[In, Out](
        extracted: ExtractedFact[In],
        convert: Callable[[In], Out | None],
        stated_in: Callable[[Out, str], bool] | None = None,
    ) -> Fact[Out]:
        return _fact(extracted, convert, document, stated_in)

    coverage = output.coverage
    eligibility = output.eligibility
    try:
        return PolicyExtraction(
            provider=fact(output.provider, _provider_name),
            scheme_name=fact(output.scheme_name, _text),
            annual_premium=fact(output.annual_premium, _money, quote_states_amount),
            annual_excess=fact(output.annual_excess, _money, quote_states_amount),
            effective_date=fact(output.effective_date, _same, quote_states_date),
            renewal_date=fact(output.renewal_date, _same, quote_states_date),
            dependants_allowed=fact(output.dependants_allowed, _same),
            dependants_included=fact(output.dependants_included, _same),
            coverage=Coverage(
                inpatient=fact(coverage.inpatient, _coverage_terms),
                outpatient=fact(coverage.outpatient, _coverage_terms),
                diagnostics=fact(coverage.diagnostics, _coverage_terms),
                physiotherapy=fact(coverage.physiotherapy, _coverage_terms),
                mental_health=fact(coverage.mental_health, _coverage_terms),
                cancer=fact(coverage.cancer, _coverage_terms),
            ),
            eligibility=Eligibility(
                employment_type=fact(eligibility.employment_type, _text),
                country=fact(eligibility.country, _text),
                minimum_service_months=fact(eligibility.minimum_service_months, _same),
                minimum_grade=fact(eligibility.minimum_grade, _text),
            ),
        )
    except ValidationError as error:
        raise ExtractionFailedError("The model's result failed validation.") from error


def _fact[In, Out](
    extracted: ExtractedFact[In],
    convert: Callable[[In], Out | None],
    document: NormalisedDocument,
    stated_in: Callable[[Out, str], bool] | None,
) -> Fact[Out]:
    value = None if extracted.value is None else convert(extracted.value)
    quote = _text(extracted.quote) if extracted.quote is not None else None

    issues: list[FactIssue] = []
    evidence = None
    if quote is not None:
        evidence = find_evidence(document, quote)
        if evidence is None:
            issues.append(FactIssue.EVIDENCE_NOT_FOUND)
        elif value is not None and stated_in is not None and not stated_in(value, evidence.quote):
            # Catches a real quote paired with a misread value, e.g. "£1,500" reported as 150.
            issues.append(FactIssue.VALUE_NOT_IN_EVIDENCE)
    elif value is not None:
        issues.append(FactIssue.EVIDENCE_MISSING)
    if extracted.ambiguous:
        issues.append(FactIssue.AMBIGUOUS)

    return Fact(
        value=value,
        confidence=min(max(extracted.confidence, 0.0), 1.0),
        evidence=evidence,
        issues=tuple(issues),
    )


def _same[T](value: T) -> T:
    return value


def _text(value: str) -> str | None:
    if _CONTROL_CHARACTERS.search(value):
        # Seen when a model garbles the JSON escape for a character such as "£". The text
        # after it is unreliable too (digits change), so the whole result is rejected.
        raise ExtractionFailedError("The model returned corrupted text.")
    return value.strip() or None


def _provider_name(value: str) -> str | None:
    name = _text(value)
    if name is None:
        return None
    return _LEGAL_SUFFIX.sub("", name).strip() or name


def _money(value: float) -> Decimal:
    return Decimal(str(value)).quantize(_PENNY)


def _coverage_terms(terms: ExtractedCoverageTerms) -> CoverageTerms:
    return CoverageTerms(
        covered=terms.covered,
        limit=None if terms.limit is None else _text(terms.limit),
        session_limit=terms.session_limit,
    )
