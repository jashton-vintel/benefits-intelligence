class DocumentProcessingError(Exception):
    """A failure that retrying will not fix. ``code`` is reported back to the API."""

    code = "PROCESSING_FAILED"

    def __init__(self, message: str) -> None:
        super().__init__(message)
        self.message = message


class InvalidDocumentError(DocumentProcessingError):
    code = "INVALID_DOCUMENT"
