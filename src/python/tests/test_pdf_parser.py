from pathlib import Path

import pymupdf
import pytest

from app.services.errors import InvalidDocumentError
from app.services.pdf_parser import parse_pdf

SAMPLE_DATA = Path(__file__).resolve().parents[3] / "sample-data"
CURRENT_POLICY = SAMPLE_DATA / "CurrentHealthPolicy.pdf"


def write_pdf(path: Path, *, text: str | None = None, password: str | None = None) -> Path:
    document = pymupdf.open()
    page = document.new_page()
    if text:
        page.insert_text((72, 72), text)

    if password:
        document.save(
            path,
            # Exported at runtime but missing from PyMuPDF's type information.
            encryption=pymupdf.PDF_ENCRYPT_AES_256,  # pyright: ignore[reportAttributeAccessIssue]
            user_pw=password,
            owner_pw=password + "-owner",
        )
    else:
        document.save(path)

    document.close()
    return path


def test_returns_every_page_in_order_with_one_based_numbers() -> None:
    pages = parse_pdf(CURRENT_POLICY)

    assert [page.page_number for page in pages] == list(range(1, len(pages) + 1))
    assert len(pages) > 1
    assert "Corporate Plus" in pages[0].text


def test_keeps_text_on_the_page_it_appears() -> None:
    pages = parse_pdf(CURRENT_POLICY)

    complaints_pages = [page.page_number for page in pages if "Complaints" in page.text]

    assert complaints_pages == [pages[-1].page_number]


def test_parses_a_simple_single_page_document(tmp_path: Path) -> None:
    pages = parse_pdf(write_pdf(tmp_path / "simple.pdf", text="Members are covered."))

    assert len(pages) == 1
    assert "Members are covered." in pages[0].text


@pytest.mark.parametrize(
    ("content", "reason"),
    [
        (b"This is plain text, not a PDF.", "not a readable PDF"),
        (b"", "not a readable PDF"),
        (CURRENT_POLICY.read_bytes()[:3000], "damaged"),
        (CURRENT_POLICY.read_bytes()[: CURRENT_POLICY.stat().st_size // 2], "no extractable text"),
    ],
    ids=["plain-text", "empty", "truncated", "half-written"],
)
def test_rejects_unreadable_files(tmp_path: Path, content: bytes, reason: str) -> None:
    path = tmp_path / "upload.pdf"
    path.write_bytes(content)

    with pytest.raises(InvalidDocumentError, match=reason) as error:
        parse_pdf(path)

    assert error.value.code == "INVALID_DOCUMENT"


def test_rejects_password_protected_document(tmp_path: Path) -> None:
    path = write_pdf(tmp_path / "locked.pdf", text="Confidential", password="secret")

    with pytest.raises(InvalidDocumentError, match="password protected"):
        parse_pdf(path)


def test_rejects_document_without_a_text_layer(tmp_path: Path) -> None:
    path = write_pdf(tmp_path / "scanned.pdf")

    with pytest.raises(InvalidDocumentError, match="no extractable text"):
        parse_pdf(path)


def test_missing_file_is_not_treated_as_an_invalid_document(tmp_path: Path) -> None:
    with pytest.raises(FileNotFoundError):
        parse_pdf(tmp_path / "missing.pdf")
