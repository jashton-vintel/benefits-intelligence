from app.models.document import PageContent
from app.services.document_chunker import chunk_document
from app.services.text_normaliser import NormalisedDocument, normalise_document
from app.services.tokens import count_tokens


def document_of(*texts: str) -> NormalisedDocument:
    return normalise_document(
        [PageContent(page_number=n, text=t) for n, t in enumerate(texts, start=1)]
    )


def test_short_document_is_a_single_chunk() -> None:
    chunks = chunk_document(document_of("One sentence. Another sentence."))

    assert len(chunks) == 1
    assert chunks[0].text == "One sentence. Another sentence."
    assert (chunks[0].page_start, chunks[0].page_end) == (1, 1)


def test_chunks_stay_within_token_budget_and_never_split_sentences() -> None:
    sentences = [f"Sentence number {i} describes a benefit." for i in range(40)]
    chunks = chunk_document(document_of(" ".join(sentences)), max_tokens=40, overlap_sentences=0)

    assert len(chunks) > 1
    assert all(chunk.token_count <= 40 for chunk in chunks)
    assert all(chunk.text.endswith("benefit.") for chunk in chunks)
    assert " ".join(chunk.text for chunk in chunks) == " ".join(sentences)


def test_token_count_matches_the_chunk_text() -> None:
    sentences = " ".join(f"Clause {i} applies to the member." for i in range(20))

    for chunk in chunk_document(document_of(sentences), max_tokens=30):
        assert chunk.token_count == count_tokens(chunk.text)


def test_consecutive_chunks_overlap_by_a_sentence() -> None:
    sentences = [f"Clause {i} applies to every member." for i in range(30)]
    chunks = chunk_document(document_of(" ".join(sentences)), max_tokens=30, overlap_sentences=1)

    for previous, following in zip(chunks, chunks[1:], strict=False):
        last_sentence = previous.text.rsplit(". ", 1)[-1]
        assert following.text.startswith(last_sentence)


def test_oversized_sentence_becomes_its_own_chunk() -> None:
    long_sentence = "This clause " + "keeps going " * 50 + "until it ends."
    chunks = chunk_document(document_of(f"Short one. {long_sentence} Short two."), max_tokens=20)

    assert any(chunk.text == long_sentence for chunk in chunks)


def test_chunk_records_the_pages_it_spans() -> None:
    chunks = chunk_document(document_of("Starts on page one and", "finishes on page two. Next."))

    assert (chunks[0].page_start, chunks[0].page_end) == (1, 2)


def test_chunk_indexes_are_sequential() -> None:
    sentences = " ".join(f"Item {i} is listed here." for i in range(50))
    chunks = chunk_document(document_of(sentences), max_tokens=25)

    assert [chunk.index for chunk in chunks] == list(range(len(chunks)))
