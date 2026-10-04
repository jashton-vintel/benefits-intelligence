import json
from pathlib import Path

import pytest

from app.models.messages import ProcessPolicyRequested
from app.services.errors import DocumentNotFoundError, InvalidDocumentError
from app.workers.policy_worker import failure_for, handle_process_requested

ROOT = Path(__file__).resolve().parents[3]
FIXTURES = ROOT / "contracts" / "fixtures"
SAMPLE_POLICY = ROOT / "sample-data" / "CurrentHealthPolicy.pdf"


def request_for(location: str) -> ProcessPolicyRequested:
    payload = json.loads((FIXTURES / "process_requested.json").read_text(encoding="utf-8"))
    payload["document_location"] = location
    return ProcessPolicyRequested.model_validate(payload)


def store_document(root: Path, location: str, content: bytes) -> None:
    path = root / location
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(content)


async def test_completion_reports_pages_and_chunks_of_the_document(tmp_path: Path) -> None:
    location = "documents/org/policy.pdf"
    store_document(tmp_path, location, SAMPLE_POLICY.read_bytes())
    request = request_for(location)

    completed = await handle_process_requested(request, tmp_path)

    assert completed.correlation_id == request.correlation_id
    assert completed.tenant_id == request.tenant_id
    assert completed.policy_id == request.policy_id
    assert completed.message_id != request.message_id
    assert completed.extraction["page_count"] == 7
    assert completed.extraction["chunk_count"] > 1


async def test_invalid_document_is_reported_as_such(tmp_path: Path) -> None:
    store_document(tmp_path, "documents/org/upload.pdf", b"%PDF-1.7 but nothing else")

    with pytest.raises(InvalidDocumentError):
        await handle_process_requested(request_for("documents/org/upload.pdf"), tmp_path)


def test_failure_carries_request_identity_and_error_details() -> None:
    request = request_for("documents/org/upload.pdf")

    failed = failure_for(request, InvalidDocumentError("The PDF is password protected."))

    assert failed.correlation_id == request.correlation_id
    assert failed.tenant_id == request.tenant_id
    assert failed.policy_id == request.policy_id
    assert failed.message_id != request.message_id
    assert failed.status == "failed"
    assert failed.error_code == "INVALID_DOCUMENT"
    assert failed.error_message == "The PDF is password protected."


async def test_location_outside_the_store_is_refused(tmp_path: Path) -> None:
    store = tmp_path / "store"
    store.mkdir()
    store_document(tmp_path, "secret.pdf", SAMPLE_POLICY.read_bytes())

    with pytest.raises(DocumentNotFoundError):
        await handle_process_requested(request_for("../secret.pdf"), store)
