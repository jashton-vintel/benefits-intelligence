import re
from bisect import bisect_right
from collections import Counter
from dataclasses import dataclass

import ftfy

from app.models.document import PageContent

# Running headers and footers are detected only when there are enough pages for a
# line repeating on most of them to be meaningful.
_MIN_PAGES_FOR_RUNNING_LINES = 3
_DIGITS = re.compile(r"\d+")
_LINE_END_HYPHEN = re.compile(r"(?<=\w)-\n(?=\w)")
_WHITESPACE = re.compile(r"\s+")


@dataclass(frozen=True)
class EvidenceLocation:
    page_start: int
    page_end: int


@dataclass(frozen=True)
class NormalisedDocument:
    """Clean document text with page boundaries, so any span maps back to its pages."""

    text: str
    page_offsets: tuple[int, ...]

    def page_at(self, offset: int) -> int:
        return bisect_right(self.page_offsets, offset)

    def locate(self, quote: str) -> EvidenceLocation | None:
        needle = normalise_text(quote)
        start = self.text.find(needle) if needle else -1
        if start < 0:
            return None
        return EvidenceLocation(self.page_at(start), self.page_at(start + len(needle) - 1))


def normalise_text(text: str) -> str:
    """Normalise free text the same way document text is, for comparisons."""
    # ftfy repairs ligatures, mis-decoded characters and curly quotes without NFKC's side
    # effects on symbols such as "½" and "m²".
    text = ftfy.fix_text(text)
    text = _LINE_END_HYPHEN.sub("-", text)
    return _WHITESPACE.sub(" ", text).strip()


def normalise_document(pages: list[PageContent]) -> NormalisedDocument:
    page_lines = [ftfy.fix_text(page.text).splitlines() for page in pages]
    running = _running_lines(page_lines)

    parts: list[str] = []
    offsets: list[int] = []
    position = 0

    for lines in page_lines:
        kept = "\n".join(line for line in lines if _mask(line) not in running)
        page_text = normalise_text(kept)

        offsets.append(position)
        parts.append(page_text)
        position += len(page_text) + 1

    return NormalisedDocument(text=" ".join(parts), page_offsets=tuple(offsets))


def _running_lines(page_lines: list[list[str]]) -> set[str]:
    if len(page_lines) < _MIN_PAGES_FOR_RUNNING_LINES:
        return set()

    occurrences = Counter(
        masked for lines in page_lines for masked in {_mask(line) for line in lines}
    )
    return {line for line, count in occurrences.items() if line and count > len(page_lines) / 2}


def _mask(line: str) -> str:
    # "Page 2 of 5" and "Page 3 of 5" should count as the same running line.
    return _DIGITS.sub("#", line.strip())
