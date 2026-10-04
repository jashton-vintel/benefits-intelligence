import re
from bisect import bisect_right
from collections import Counter
from dataclasses import dataclass
from functools import cached_property

import ftfy

from app.models.document import PageContent

# Running headers and footers are detected only when there are enough pages for a
# line repeating on most of them to be meaningful.
_MIN_PAGES_FOR_RUNNING_LINES = 3
_DIGITS = re.compile(r"\d+")
_LINE_END_HYPHEN = re.compile(r"(?<=\w)-\n(?=\w)")
_LINE_BREAKS = re.compile(r"\s*\n\s*")
_SPACES = re.compile(r"[^\S\n]+")
_WHITESPACE = re.compile(r"\s+")


@dataclass(frozen=True)
class EvidenceLocation:
    page_start: int
    page_end: int


@dataclass(frozen=True)
class NormalisedDocument:
    """Clean document text with page boundaries, so any span maps back to its pages.

    Line breaks are kept, one per break, because layout carries meaning: a title on its own
    line is not the start of the following sentence. Searches treat a line break as a space.
    """

    text: str
    page_offsets: tuple[int, ...]

    @cached_property
    def _searchable(self) -> str:
        # Same length as the text, so offsets found here are valid offsets into it.
        return self.text.replace("\n", " ")

    def page_at(self, offset: int) -> int:
        return bisect_right(self.page_offsets, offset)

    def locate(self, quote: str) -> EvidenceLocation | None:
        needle = flatten(normalise_text(quote))
        start = self._searchable.find(needle) if needle else -1
        if start < 0:
            return None
        return EvidenceLocation(self.page_at(start), self.page_at(start + len(needle) - 1))


def normalise_text(text: str) -> str:
    """Repair the text and tidy whitespace, keeping a single line break between lines."""
    # ftfy repairs ligatures, mis-decoded characters and curly quotes without NFKC's side
    # effects on symbols such as "½" and "m²".
    text = ftfy.fix_text(text)
    text = _LINE_END_HYPHEN.sub("-", text)
    text = _LINE_BREAKS.sub("\n", text)
    return _SPACES.sub(" ", text).strip()


def flatten(text: str) -> str:
    """Collapse all whitespace, including line breaks, to single spaces."""
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

    return NormalisedDocument(text="\n".join(parts), page_offsets=tuple(offsets))


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
