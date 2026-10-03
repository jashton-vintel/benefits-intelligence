import json
from pathlib import Path

from app.models.messages import ProcessPolicyRequested
from app.workers.policy_worker import handle_process_requested

FIXTURES = Path(__file__).resolve().parents[3] / "contracts" / "fixtures"


def test_completion_carries_request_identity_with_new_message_id() -> None:
    request = ProcessPolicyRequested.model_validate(
        json.loads((FIXTURES / "process_requested.json").read_text(encoding="utf-8"))
    )

    completed = handle_process_requested(request)

    assert completed.correlation_id == request.correlation_id
    assert completed.tenant_id == request.tenant_id
    assert completed.policy_id == request.policy_id
    assert completed.message_id != request.message_id
    assert completed.status == "completed"
