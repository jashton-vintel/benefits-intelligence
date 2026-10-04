from datetime import date
from decimal import Decimal

from app.models.comparison import ComparisonSummaryRequest, FieldDifference

_FIELD_LABELS = {
    "provider": "Provider",
    "scheme_name": "Scheme name",
    "annual_premium": "Annual premium",
    "annual_excess": "Annual excess",
    "effective_date": "Effective date",
    "renewal_date": "Renewal date",
    "dependants_allowed": "Dependants can join",
    "dependants_included": "Dependant cover paid by the employer",
}

_COVERAGE_LABELS = {
    "inpatient": "In-patient and day-patient treatment",
    "outpatient": "Out-patient treatment",
    "diagnostics": "Diagnostics",
    "physiotherapy": "Physiotherapy",
    "mental_health": "Mental health",
    "cancer": "Cancer care",
}

_COVERAGE_PARTS = {
    "covered": "covered",
    "sessions": "sessions per year",
    "limit": "terms",
}

_ELIGIBILITY_LABELS = {
    "employment_type": "Eligible employment type",
    "country": "Eligible country of residence",
    "minimum_service_months": "Minimum service in months",
    "minimum_grade": "Minimum grade",
}


def describe_comparison(request: ComparisonSummaryRequest) -> list[str]:
    """One plain statement per field, with every figure already formatted.

    The wording is produced here rather than by the model, so the figures a summary can use
    are exactly the ones in these lines.
    """
    return [describe_difference(difference) for difference in request.differences]


def describe_difference(difference: FieldDifference) -> str:
    label = field_label(difference.field)
    current = _format(difference, difference.current)
    proposed = _format(difference, difference.proposed)
    review = " Not yet confirmed: needs review." if difference.needs_review else ""

    match difference.change:
        case "unchanged":
            return f"{label}: {current} in both.{review}"
        case "increased" | "decreased":
            return f"{label}: {current} → {proposed} ({_delta(difference)}).{review}"
        case "removed":
            return f"{label}: {current} → no requirement (requirement removed).{review}"
        case "added":
            return f"{label}: no requirement → {proposed} (requirement added).{review}"
        case "unknown":
            return f"{label}: {current} → {proposed} (cannot be compared).{review}"
        case _:
            return f"{label}: {current} → {proposed} (changed).{review}"


def field_label(path: str) -> str:
    group, _, rest = path.partition(".")
    if group == "coverage":
        kind, _, part = rest.partition(".")
        cover = _COVERAGE_LABELS.get(kind, _humanise(kind))
        return f"{cover} {_COVERAGE_PARTS.get(part, _humanise(part))}"
    if group == "eligibility":
        return _ELIGIBILITY_LABELS.get(rest, _humanise(rest))
    return _FIELD_LABELS.get(path, _humanise(path))


def _format(difference: FieldDifference, value: str | None) -> str:
    if value is None:
        return "not stated"
    match difference.kind:
        case "money":
            return _money(Decimal(value))
        case "date":
            parsed = date.fromisoformat(value)
            return f"{parsed.day} {parsed:%B %Y}"
        case "flag":
            return "yes" if value == "true" else "no"
        case "text":
            return f'"{value}"'
        case _:
            return value


def _delta(difference: FieldDifference) -> str:
    direction = difference.change
    if difference.delta is None:
        return direction
    amount = abs(difference.delta)
    size = _money(amount) if difference.kind == "money" else f"{amount:,.0f}"
    return f"{direction} by {size}"


def _money(amount: Decimal) -> str:
    return f"£{amount:,.0f}" if amount == amount.to_integral_value() else f"£{amount:,.2f}"


def _humanise(key: str) -> str:
    return key.replace("_", " ").capitalize()
