FROM python:3.13-slim

COPY --from=ghcr.io/astral-sh/uv:0.12.22 /uv /bin/uv

ENV PYTHONUNBUFFERED=1 \
    UV_COMPILE_BYTECODE=1 \
    UV_LINK_MODE=copy \
    UV_PYTHON_DOWNLOADS=never

WORKDIR /app

# Dependencies first so this layer is reused until the lockfile changes.
COPY pyproject.toml uv.lock ./
RUN uv sync --locked --no-dev --no-install-project

COPY app ./app
RUN uv sync --locked --no-dev --no-editable

# Same UID as the .NET runtime image's app user, so either container can initialise the
# shared document volume with ownership the other can use.
RUN useradd --uid 1654 --no-create-home app \
    && mkdir /data \
    && chown app /data

ENV PATH="/app/.venv/bin:$PATH"

USER app
CMD ["policy-worker"]
