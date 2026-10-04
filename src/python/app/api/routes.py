import logging
from typing import Annotated

from fastapi import APIRouter, Depends, HTTPException, Request, status

from app.api.security import require_internal_key
from app.models.comparison import ComparisonSummaryRequest, ComparisonSummaryResponse
from app.services.comparison_summariser import ComparisonSummariser, SummaryUnavailableError

logger = logging.getLogger(__name__)
router = APIRouter()


def get_summariser(request: Request) -> ComparisonSummariser:
    return request.app.state.summariser


@router.get("/health")
async def health() -> dict[str, str]:
    return {"status": "healthy"}


@router.post(
    "/summaries/comparison",
    dependencies=[Depends(require_internal_key)],
    responses={
        status.HTTP_503_SERVICE_UNAVAILABLE: {"description": "No summary could be produced."}
    },
)
async def summarise_comparison(
    comparison: ComparisonSummaryRequest,
    summariser: Annotated[ComparisonSummariser, Depends(get_summariser)],
) -> ComparisonSummaryResponse:
    try:
        summary = await summariser.summarise(comparison)
    except SummaryUnavailableError as error:
        logger.warning("No summary for comparison: %s", error)
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail=str(error)
        ) from error
    logger.info(
        "Summarised comparison of %s with %s", comparison.current.id, comparison.proposed.id
    )
    return ComparisonSummaryResponse(summary=summary)
