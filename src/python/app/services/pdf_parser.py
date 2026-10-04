from pathlib import Path

import pymupdf

from app.models.document import PageContent
from app.services.errors import InvalidDocumentError

# MuPDF writes repair warnings straight to stderr, bypassing our structured logs.
# Failures still surface as exceptions, which is all the parser relies on.
pymupdf.TOOLS.mupdf_display_errors(False)


def parse_pdf(path: Path) -> list[PageContent]:
    """Extract the text of each page, keeping page numbers for evidence references.

    Raises InvalidDocumentError for files that cannot yield usable text. A missing file
    raises FileNotFoundError, since that is a storage problem rather than a bad document.
    """
    if not path.is_file():
        raise FileNotFoundError(f"Document not found: {path}")

    try:
        document = pymupdf.open(path, filetype="pdf")
    except RuntimeError as error:
        # PyMuPDF's FileDataError and EmptyFileError both derive from RuntimeError.
        raise InvalidDocumentError("The file is not a readable PDF document.") from error

    with document:
        if document.needs_pass:
            raise InvalidDocumentError("The PDF is password protected.")

        try:
            pages = [
                PageContent(page_number=index + 1, text=_page_text(document, index))
                for index in range(document.page_count)
            ]
        except RuntimeError as error:
            raise InvalidDocumentError(
                "The PDF is damaged and its pages cannot be read."
            ) from error

    if not any(page.text.strip() for page in pages):
        # Typically a scanned document with no text layer, or a damaged file that MuPDF
        # repaired into empty pages. OCR is out of scope.
        raise InvalidDocumentError("The PDF contains no extractable text.")

    return pages


def _page_text(document: pymupdf.Document, index: int) -> str:
    text = document.load_page(index).get_text("text")
    if not isinstance(text, str):
        raise TypeError(f"Expected plain text from page {index + 1}, got {type(text).__name__}.")
    return text
