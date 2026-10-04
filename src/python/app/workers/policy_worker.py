import asyncio
import logging
from pathlib import Path
from uuid import uuid4

from aio_pika.abc import AbstractExchange, AbstractIncomingMessage

from app.config import load_settings
from app.logging_config import configure_logging, log_context
from app.messaging.publisher import publish_completed, publish_failed
from app.messaging.rabbit_connection import connect
from app.messaging.topology import declare_topology
from app.models.messages import ProcessPolicyCompleted, ProcessPolicyFailed, ProcessPolicyRequested
from app.services.document_paths import resolve_document_path
from app.services.document_preparation import prepare_document
from app.services.errors import DocumentProcessingError

logger = logging.getLogger(__name__)


async def handle_process_requested(
    request: ProcessPolicyRequested, document_root: Path
) -> ProcessPolicyCompleted:
    path = resolve_document_path(document_root, request.document_location)

    # Parsing is CPU-bound and blocking; running it on the event loop would stall the
    # RabbitMQ connection's heartbeats and every other coroutine until it finished.
    prepared = await asyncio.to_thread(prepare_document, path)

    logger.info("Processed %d pages (%d chunks)", prepared.page_count, len(prepared.chunks))

    return ProcessPolicyCompleted(
        message_id=uuid4(),
        correlation_id=request.correlation_id,
        tenant_id=request.tenant_id,
        policy_id=request.policy_id,
        extraction={"page_count": prepared.page_count, "chunk_count": len(prepared.chunks)},
    )


def failure_for(
    request: ProcessPolicyRequested, error: DocumentProcessingError
) -> ProcessPolicyFailed:
    return ProcessPolicyFailed(
        message_id=uuid4(),
        correlation_id=request.correlation_id,
        tenant_id=request.tenant_id,
        policy_id=request.policy_id,
        error_code=error.code,
        error_message=error.message,
    )


async def on_message(
    message: AbstractIncomingMessage, exchange: AbstractExchange, document_root: Path
) -> None:
    # IDs come from the AMQP properties rather than the body,
    # so messages that fail to parse are still traceable.
    with log_context(correlation_id=message.correlation_id, message_id=message.message_id):
        try:
            # The request is acked only after the result is confirmed by the broker.
            # Any exception escaping the block rejects the message instead.
            async with message.process():
                request = ProcessPolicyRequested.model_validate_json(message.body)
                logger.info("Processing policy %s", request.policy_id)

                try:
                    completed = await handle_process_requested(request, document_root)
                except DocumentProcessingError as error:
                    # A document that can never be processed is a result, not a delivery
                    # failure: report it so the job is marked failed, then ack the request.
                    logger.warning(
                        "Policy %s failed: %s (%s)", request.policy_id, error.message, error.code
                    )
                    await publish_failed(exchange, failure_for(request, error))
                    return

                await publish_completed(exchange, completed)
                logger.info("Completed policy %s", request.policy_id)
        except Exception:
            # process() has already rejected the message; keep consuming.
            logger.exception("Failed to process message")


async def run() -> None:
    settings = load_settings()
    connection = await connect(settings)
    async with connection:
        channel = await connection.channel()
        await channel.set_qos(prefetch_count=1)
        exchange, queue = await declare_topology(channel)

        logger.info(
            "Waiting for messages on %s (documents in %s)", queue.name, settings.document_root
        )
        async with queue.iterator() as messages:
            async for message in messages:
                await on_message(message, exchange, settings.document_root)


def main() -> None:
    configure_logging()
    try:
        asyncio.run(run())
    except KeyboardInterrupt:
        pass


if __name__ == "__main__":
    main()
