from dataclasses import dataclass
from pathlib import Path

from app.models.document import DocumentChunk, PageContent
from app.services.document_chunker import chunk_document
from app.services.pdf_parser import parse_pdf
from app.services.text_normaliser import NormalisedDocument, normalise_document


@dataclass(frozen=True)
class PreparedDocument:
    pages: list[PageContent]
    document: NormalisedDocument
    chunks: list[DocumentChunk]

    @property
    def page_count(self) -> int:
        return len(self.pages)


def prepare_document(path: Path) -> PreparedDocument:
    """Run the text stages of the pipeline. CPU-bound and blocking; call it off the event loop."""
    pages = parse_pdf(path)
    document = normalise_document(pages)
    return PreparedDocument(pages=pages, document=document, chunks=chunk_document(document))
