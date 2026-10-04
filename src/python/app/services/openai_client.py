from openai import AsyncOpenAI

from app.config import OpenAISettings


def create_openai_client(settings: OpenAISettings) -> AsyncOpenAI:
    api_key = settings.openai_api_key.get_secret_value() if settings.openai_api_key else ""
    if not api_key.strip():
        raise RuntimeError("OPENAI_API_KEY must be set.")

    return AsyncOpenAI(api_key=api_key, timeout=settings.openai_timeout_seconds, max_retries=3)
