from pathlib import Path

import pytest

from app.services.document_paths import resolve_document_path
from app.services.errors import DocumentNotFoundError


@pytest.fixture
def store(tmp_path: Path) -> Path:
    root = tmp_path / "store"
    (root / "documents" / "org").mkdir(parents=True)
    (root / "documents" / "org" / "policy.pdf").write_bytes(b"%PDF-1.7")
    (tmp_path / "outside.pdf").write_bytes(b"%PDF-1.7")
    return root


def test_resolves_relative_location_inside_the_store(store: Path) -> None:
    path = resolve_document_path(store, "documents/org/policy.pdf")

    assert path == (store / "documents" / "org" / "policy.pdf").resolve()


@pytest.mark.parametrize(
    "location",
    [
        "../outside.pdf",
        "documents/../../outside.pdf",
        "/etc/passwd",
        "C:/Windows/win.ini",
        "",
    ],
    ids=["parent", "nested-parent", "absolute-posix", "absolute-windows", "empty"],
)
def test_refuses_locations_outside_the_store(store: Path, location: str) -> None:
    with pytest.raises(DocumentNotFoundError) as error:
        resolve_document_path(store, location)

    assert error.value.code == "DOCUMENT_NOT_FOUND"


def test_reports_missing_document(store: Path) -> None:
    with pytest.raises(DocumentNotFoundError, match="not found"):
        resolve_document_path(store, "documents/org/missing.pdf")
