from app.models.document import DocumentChunk
from app.services.retrieval import retrieve


def chunk(index: int, text: str) -> DocumentChunk:
    return DocumentChunk(index=index, text=text, token_count=10, page_start=1, page_end=1)


CHUNKS = [
    chunk(0, "The scheme is open to all employees resident in the United Kingdom."),
    chunk(1, "An excess of £150 applies to each covered person once in each scheme year."),
    chunk(2, "Physiotherapy is covered for up to ten sessions per covered person."),
    chunk(3, "Mental health treatment is covered in full."),
    chunk(4, "Claims can be made through the member app."),
    chunk(5, "Osteopathy and chiropractic treatment are not covered under this scheme."),
]


def test_returns_the_most_relevant_chunks_in_document_order() -> None:
    relevant = retrieve(CHUNKS, "How many physiotherapy sessions are covered?", limit=2)

    assert CHUNKS[2] in relevant
    assert relevant == sorted(relevant, key=lambda c: c.index)


def test_finds_the_chunk_that_answers_the_question() -> None:
    relevant = retrieve(CHUNKS, "What is the excess?", limit=1)

    assert relevant == [CHUNKS[1]]


def test_small_documents_are_used_whole() -> None:
    assert retrieve(CHUNKS[:3], "anything", limit=5) == CHUNKS[:3]


def test_ignores_common_words_when_ranking() -> None:
    relevant = retrieve(CHUNKS, "Does the scheme cover osteopathy?", limit=1)

    assert relevant == [CHUNKS[5]]
