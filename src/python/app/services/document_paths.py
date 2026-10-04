from pathlib import Path, PurePosixPath

from app.services.errors import DocumentNotFoundError


def resolve_document_path(root: Path, location: str) -> Path:
    """Map a message's relative document location onto the local document store.

    The location comes from a message, so it is treated as untrusted: absolute paths and
    anything resolving outside the store root are refused.
    """
    relative = PurePosixPath(location)
    if not location or relative.is_absolute() or ":" in location:
        raise DocumentNotFoundError(f"Document location '{location}' is not a relative path.")

    store = root.resolve()
    path = store.joinpath(*relative.parts).resolve()

    if not path.is_relative_to(store):
        raise DocumentNotFoundError(f"Document location '{location}' is outside the store.")

    if not path.is_file():
        raise DocumentNotFoundError(f"Document '{location}' was not found in the store.")

    return path
