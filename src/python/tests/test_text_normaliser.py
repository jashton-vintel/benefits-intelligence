from app.models.document import PageContent
from app.services.text_normaliser import normalise_document, normalise_text


def pages(*texts: str) -> list[PageContent]:
    return [
        PageContent(page_number=number, text=text) for number, text in enumerate(texts, start=1)
    ]


def test_replaces_ligatures_and_collapses_whitespace() -> None:
    assert (
        normalise_text("Cover is conﬁrmed  for   the\nﬂoor") == "Cover is confirmed for the floor"
    )


def test_repairs_mis_decoded_characters_and_straightens_quotes() -> None:
    assert normalise_text("cafÃ© “plan” it’s") == 'café "plan" it\'s'


def test_preserves_fractions_and_superscripts() -> None:
    assert normalise_text("½ day, 10 m²") == "½ day, 10 m²"


def test_rejoins_words_hyphenated_across_lines() -> None:
    assert normalise_text("accessed by self-\nreferral") == "accessed by self-referral"


def test_keeps_hyphens_that_are_not_line_breaks() -> None:
    assert normalise_text("in-patient and day - patient") == "in-patient and day - patient"


def test_removes_running_headers_and_page_numbers() -> None:
    document = normalise_document(
        pages(
            "Acme Health | Scheme\nFirst page body.\nPage 1 of 3",
            "Acme Health | Scheme\nSecond page body.\nPage 2 of 3",
            "Acme Health | Scheme\nThird page body.\nPage 3 of 3",
        )
    )

    assert document.text == "First page body. Second page body. Third page body."


def test_keeps_repeated_lines_in_short_documents() -> None:
    document = normalise_document(pages("Acme Health\nIntro.", "Acme Health\nTerms."))

    assert document.text == "Acme Health Intro. Acme Health Terms."


def test_maps_offsets_back_to_pages() -> None:
    document = normalise_document(pages("Alpha beta.", "Gamma delta.", "Epsilon."))

    assert document.page_at(document.text.index("Alpha")) == 1
    assert document.page_at(document.text.index("Gamma")) == 2
    assert document.page_at(document.text.index("Epsilon")) == 3


def test_locates_quote_spanning_a_page_break() -> None:
    document = normalise_document(
        pages(
            "Header\nCost arrangements for the\nPage 1",
            "Header\ncost of cover will be confirmed.\nPage 2",
            "Header\nOther terms.\nPage 3",
        )
    )

    location = document.locate("arrangements for the cost of cover will be confirmed")

    assert location is not None
    assert (location.page_start, location.page_end) == (1, 2)


def test_locate_returns_none_for_missing_or_empty_quote() -> None:
    document = normalise_document(pages("Physiotherapy is covered."))

    assert document.locate("IVF") is None
    assert document.locate("   ") is None
