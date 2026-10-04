import re

from app.models.document import DocumentChunk
from app.services.text_normaliser import NormalisedDocument
from app.services.tokens import count_tokens

_SENTENCE_BREAK = re.compile(r"(?<=[.!?])\s+")

Span = tuple[int, int]


def chunk_document(
    document: NormalisedDocument,
    max_tokens: int = 350,
    overlap_sentences: int = 1,
) -> list[DocumentChunk]:
    """Split the document into chunks of whole sentences within a token budget.

    Consecutive chunks share ``overlap_sentences`` so a fact at a boundary appears whole in at
    least one chunk. Sentences are never split, so one longer than the budget becomes its own
    chunk. Chunks may span pages; each records the first and last page it draws from.
    """
    text = document.text

    def fits(sentences: list[Span]) -> bool:
        return count_tokens(text[sentences[0][0] : sentences[-1][1]]) <= max_tokens

    chunks: list[DocumentChunk] = []
    current: list[Span] = []

    for sentence in _sentence_spans(text):
        if current and not fits([*current, sentence]):
            chunks.append(_to_chunk(document, current, index=len(chunks)))
            current = current[-overlap_sentences:] if overlap_sentences else []
            if current and not fits([*current, sentence]):
                current = []
        current.append(sentence)

    if current:
        chunks.append(_to_chunk(document, current, index=len(chunks)))

    return chunks


def _sentence_spans(text: str) -> list[Span]:
    spans: list[Span] = []
    start = 0
    for match in _SENTENCE_BREAK.finditer(text):
        spans.append((start, match.start()))
        start = match.end()
    if start < len(text):
        spans.append((start, len(text)))
    return spans


def _to_chunk(document: NormalisedDocument, sentences: list[Span], index: int) -> DocumentChunk:
    start, end = sentences[0][0], sentences[-1][1]
    text = document.text[start:end]
    return DocumentChunk(
        index=index,
        text=text,
        token_count=count_tokens(text),
        page_start=document.page_at(start),
        page_end=document.page_at(end - 1),
    )
