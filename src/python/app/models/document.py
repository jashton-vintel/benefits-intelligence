from pydantic import BaseModel, ConfigDict, Field


class PageContent(BaseModel):
    model_config = ConfigDict(frozen=True)

    page_number: int = Field(ge=1)
    text: str


class DocumentChunk(BaseModel):
    model_config = ConfigDict(frozen=True)

    index: int = Field(ge=0)
    text: str
    token_count: int = Field(ge=0)
    page_start: int = Field(ge=1)
    page_end: int = Field(ge=1)


class DocumentSummary(BaseModel):
    model_config = ConfigDict(frozen=True)

    page_count: int = Field(ge=1)
    chunk_count: int = Field(ge=1)
