import json
from pathlib import Path

import pytest
from conftest import FakeExtractor

from app.config import load_settings
from app.models.messages import ProcessPolicyRequested
from app.services.errors import DocumentNotFoundError, InvalidDocumentError
from app.workers.policy_worker import create_extractor, failure_for, handle_process_requested

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


async def test_completion_carries_document_summary_and_extraction(
    tmp_path: Path, fake_extractor: FakeExtractor
) -> None:
    location = "documents/org/policy.pdf"
    store_document(tmp_path, location, SAMPLE_POLICY.read_bytes())
    request = request_for(location)

    completed = await handle_process_requested(request, tmp_path, fake_extractor)

    assert completed.correlation_id == request.correlation_id
    assert completed.tenant_id == request.tenant_id
    assert completed.policy_id == request.policy_id
    assert completed.message_id != request.message_id
    assert completed.document.page_count == 7
    assert completed.document.chunk_count == len(fake_extractor.received)
    assert completed.extraction == fake_extractor.result


async def test_invalid_document_is_reported_before_extraction(
    tmp_path: Path, fake_extractor: FakeExtractor
) -> None:
    store_document(tmp_path, "documents/org/upload.pdf", b"%PDF-1.7 but nothing else")

    with pytest.raises(InvalidDocumentError):
        await handle_process_requested(
            request_for("documents/org/upload.pdf"), tmp_path, fake_extractor
        )

    assert fake_extractor.received == []


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


@pytest.mark.parametrize("api_key", [None, "", "   "], ids=["unset", "empty", "blank"])
def test_worker_refuses_to_start_without_an_api_key(
    monkeypatch: pytest.MonkeyPatch, api_key: str | None
) -> None:
    monkeypatch.setenv("RABBITMQ_USER", "svc")
    monkeypatch.setenv("RABBITMQ_PASSWORD", "secret")
    if api_key is None:
        monkeypatch.delenv("OPENAI_API_KEY", raising=False)
    else:
        monkeypatch.setenv("OPENAI_API_KEY", api_key)

    with pytest.raises(RuntimeError, match="OPENAI_API_KEY"):
        create_extractor(load_settings(env_file=None))


async def test_location_outside_the_store_is_refused(
    tmp_path: Path, fake_extractor: FakeExtractor
) -> None:
    store = tmp_path / "store"
    store.mkdir()
    store_document(tmp_path, "secret.pdf", SAMPLE_POLICY.read_bytes())

    with pytest.raises(DocumentNotFoundError):
        await handle_process_requested(request_for("../secret.pdf"), store, fake_extractor)
