import hmac
from typing import Annotated

from fastapi import Depends, HTTPException, Request, status
from fastapi.security import APIKeyHeader

INTERNAL_KEY_HEADER = "X-Internal-Key"

# Declared as a security scheme so the generated docs offer a way to supply it.
_internal_key = APIKeyHeader(name=INTERNAL_KEY_HEADER, auto_error=False)


def require_internal_key(
    request: Request, key: Annotated[str | None, Depends(_internal_key)]
) -> None:
    expected: str = request.app.state.internal_api_key
    # compare_digest takes the same time however much of the key matches.
    if key is None or not hmac.compare_digest(key.encode(), expected.encode()):
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="A valid internal API key is required.",
        )
