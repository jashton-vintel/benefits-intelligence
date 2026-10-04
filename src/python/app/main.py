from collections.abc import AsyncIterator, Awaitable, Callable
from contextlib import AbstractAsyncContextManager, asynccontextmanager

import uvicorn
from fastapi import FastAPI, Request, Response

from app.api.routes import router
from app.config import ApiSettings, load_api_settings
from app.logging_config import configure_logging, log_context
from app.services.comparison_summariser import ComparisonSummariser, OpenAIComparisonSummariser
from app.services.openai_client import create_openai_client
from app.services.question_answerer import OpenAIQuestionAnswerer, QuestionAnswerer

CORRELATION_HEADER = "X-Correlation-ID"
MIN_INTERNAL_KEY_LENGTH = 32


Lifespan = Callable[[FastAPI], AbstractAsyncContextManager[None]]


def create_app(
    summariser: ComparisonSummariser,
    answerer: QuestionAnswerer,
    internal_api_key: str,
    lifespan: Lifespan | None = None,
) -> FastAPI:
    app = FastAPI(title="Benefits Intelligence AI", lifespan=lifespan)
    app.state.summariser = summariser
    app.state.answerer = answerer
    app.state.internal_api_key = internal_api_key

    @app.middleware("http")
    async def correlate(
        request: Request, call_next: Callable[[Request], Awaitable[Response]]
    ) -> Response:
        # The API sends the caller's correlation ID, so these logs join up with its own.
        with log_context(correlation_id=request.headers.get(CORRELATION_HEADER), message_id=None):
            return await call_next(request)

    app.include_router(router)
    return app


def build_app(settings: ApiSettings) -> FastAPI:
    key = settings.internal_api_key.get_secret_value() if settings.internal_api_key else ""
    if len(key.strip()) < MIN_INTERNAL_KEY_LENGTH:
        raise RuntimeError(
            f"INTERNAL_API_KEY must be set to at least {MIN_INTERNAL_KEY_LENGTH} characters."
        )

    client = create_openai_client(settings)

    @asynccontextmanager
    async def lifespan(_: FastAPI) -> AsyncIterator[None]:
        yield
        await client.close()

    return create_app(
        OpenAIComparisonSummariser(client, settings.openai_model),
        OpenAIQuestionAnswerer(client, settings.openai_model),
        key,
        lifespan,
    )


def run() -> None:
    configure_logging()
    settings = load_api_settings()
    # log_config=None keeps uvicorn on the application's JSON logging.
    uvicorn.run(
        build_app(settings), host=settings.api_host, port=settings.api_port, log_config=None
    )


if __name__ == "__main__":
    run()
