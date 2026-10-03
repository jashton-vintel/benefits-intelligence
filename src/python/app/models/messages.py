from datetime import datetime
from typing import Any, Literal
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field


class ProcessPolicyRequested(BaseModel):
    model_config = ConfigDict(frozen=True)

    message_id: UUID
    correlation_id: UUID
    schema_version: Literal["1.0"]
    tenant_id: UUID
    policy_id: UUID
    document_id: UUID
    document_location: str
    requested_at: datetime


class ProcessPolicyCompleted(BaseModel):
    model_config = ConfigDict(frozen=True)

    message_id: UUID
    correlation_id: UUID
    schema_version: Literal["1.0"] = "1.0"
    tenant_id: UUID
    policy_id: UUID
    status: Literal["completed"] = "completed"
    extraction: dict[str, Any] = Field(default_factory=dict)
