from datetime import datetime
from typing import Literal
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field

from app.models.document import DocumentSummary, PageContent
from app.models.policy import PolicyExtraction


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
    document: DocumentSummary
    extraction: PolicyExtraction
    # Raw page text, so the API can keep it for answering questions without Python holding state.
    pages: list[PageContent] = Field(min_length=1)


class ProcessPolicyFailed(BaseModel):
    model_config = ConfigDict(frozen=True)

    message_id: UUID
    correlation_id: UUID
    schema_version: Literal["1.0"] = "1.0"
    tenant_id: UUID
    policy_id: UUID
    status: Literal["failed"] = "failed"
    error_code: str
    error_message: str
