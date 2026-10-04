from pydantic import BaseModel, ConfigDict, Field

from app.models.document import PageContent


class QuestionRequest(BaseModel):
    """A question about one policy, with the policy's page text supplied by the API."""

    model_config = ConfigDict(frozen=True)

    question: str = Field(min_length=1, max_length=500)
    policy_name: str = Field(max_length=200)
    pages: list[PageContent] = Field(min_length=1, max_length=500)


class Citation(BaseModel):
    model_config = ConfigDict(frozen=True)

    page_start: int = Field(ge=1)
    page_end: int = Field(ge=1)
    quote: str


class QuestionAnswer(BaseModel):
    model_config = ConfigDict(frozen=True)

    answer: str
    supported: bool
    citations: list[Citation]


class ExtractedAnswer(BaseModel):
    """The structure requested from the model; quotes are checked before anything is returned."""

    supported: bool = Field(
        description=(
            "True only if the document passages state the answer. False if they do not "
            "address the question or only partly answer it."
        )
    )
    answer: str = Field(
        description=(
            "A direct answer in one to three sentences of plain UK English, using only what "
            "the passages state. Empty if supported is false."
        )
    )
    quotes: list[str] = Field(
        description=(
            "The passages that state the answer, each copied exactly from the document. "
            "Empty if supported is false."
        )
    )
