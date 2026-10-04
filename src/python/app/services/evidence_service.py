import calendar
import re
from datetime import date
from decimal import Decimal

from rapidfuzz import fuzz

from app.models.policy import Evidence
from app.services.text_normaliser import (
    EvidenceLocation,
    NormalisedDocument,
    flatten,
    normalise_text,
)

# Tolerates differences in case, punctuation and spacing, nothing more. Measured against the
# sample policies, a dropped word scores about 90 and a fabricated quote that reverses the
# meaning ("are covered" for "are not covered") about 89, so a looser threshold would accept
# quotes that say something the document does not.
MIN_MATCH_SCORE = 95.0


def find_evidence(document: NormalisedDocument, quote: str) -> Evidence | None:
    """Locate a quoted passage in the document and return the pages it appears on.

    Pages come from where the quote is found, never from the model, so a quote that is not
    in the document produces no evidence at all.
    """
    needle = flatten(normalise_text(quote))
    if not needle:
        return None

    location = document.locate(needle) or _closest_match(document, needle)
    if location is None:
        return None
    return Evidence(page_start=location.page_start, page_end=location.page_end, quote=needle)


def _closest_match(document: NormalisedDocument, needle: str) -> EvidenceLocation | None:
    # Lower-casing leaves the length unchanged for all but a handful of characters, so the
    # alignment maps straight back onto document offsets.
    alignment = fuzz.partial_ratio_alignment(
        needle, document.searchable, processor=str.lower, score_cutoff=MIN_MATCH_SCORE
    )
    if alignment is None or alignment.dest_end <= alignment.dest_start:
        return None
    return EvidenceLocation(
        document.page_at(alignment.dest_start), document.page_at(alignment.dest_end - 1)
    )


def quote_states_amount(amount: Decimal, quote: str) -> bool:
    """Whether the quote contains the amount, written with or without pence and separators."""
    forms = {f"{amount:,.2f}", f"{amount:.2f}"}
    if amount == amount.to_integral_value():
        forms |= {f"{amount:,.0f}", f"{amount:.0f}"}
    return any(_contains_number(quote, form) for form in forms)


def quote_states_date(value: date, quote: str) -> bool:
    """Whether the quote contains the date, written out ("1 April 2026") or numerically."""
    text = quote.casefold()
    numeric = {value.isoformat(), f"{value:%d/%m/%Y}", f"{value.day}/{value.month}/{value.year}"}
    if any(form in text for form in numeric):
        return True
    month = calendar.month_name[value.month].casefold()
    return (
        month[:3] in text
        and _contains_number(text, str(value.year))
        and _contains_number(text, str(value.day))
    )


def _contains_number(text: str, number: str) -> bool:
    # "150" must not match inside "1,500" or "150.50".
    return re.search(rf"(?<![\d,.]){re.escape(number)}(?![\d]|[,.]\d)", text) is not None
