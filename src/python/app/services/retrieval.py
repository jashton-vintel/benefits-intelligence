import re

from rank_bm25 import BM25Okapi

from app.models.document import DocumentChunk

DEFAULT_LIMIT = 5

_WORD = re.compile(r"[a-z0-9]+")
_STOP_WORDS = frozenset(
    "a an and are as at be by can do does for from has have how i if in is it its my of on or "
    "our the their this to under we what when which who will with you your".split()
)


def retrieve(
    chunks: list[DocumentChunk], question: str, limit: int = DEFAULT_LIMIT
) -> list[DocumentChunk]:
    """The chunks most relevant to the question, ranked with BM25, returned in document order.

    Keyword ranking is enough for documents of a few dozen pages and needs no index or
    embedding model; the answer is still only trusted once its quotes are found in the text.
    """
    if len(chunks) <= limit:
        return chunks

    bm25 = BM25Okapi([_terms(chunk.text) for chunk in chunks])
    scores = bm25.get_scores(_terms(question))
    ranked = sorted(range(len(chunks)), key=lambda index: scores[index], reverse=True)
    return [chunks[index] for index in sorted(ranked[:limit])]


def _terms(text: str) -> list[str]:
    return [word for word in _WORD.findall(text.lower()) if word not in _STOP_WORDS]
