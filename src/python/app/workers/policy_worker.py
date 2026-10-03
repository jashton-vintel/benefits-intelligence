import asyncio
import logging
from uuid import uuid4

from aio_pika.abc import AbstractExchange, AbstractIncomingMessage

from app.config import load_settings
from app.messaging.publisher import publish_completed
from app.messaging.rabbit_connection import connect
from app.messaging.topology import declare_topology
from app.models.messages import ProcessPolicyCompleted, ProcessPolicyRequested

logger = logging.getLogger(__name__)


def handle_process_requested(request: ProcessPolicyRequested) -> ProcessPolicyCompleted:
    return ProcessPolicyCompleted(
        message_id=uuid4(),
        correlation_id=request.correlation_id,
        tenant_id=request.tenant_id,
        policy_id=request.policy_id,
    )


async def on_message(message: AbstractIncomingMessage, exchange: AbstractExchange) -> None:
    try:
        # The request is acked only after the completion is confirmed by the broker.
        # Any exception inside the block rejects the message instead.
        async with message.process():
            request = ProcessPolicyRequested.model_validate_json(message.body)
            logger.info(
                "Processing policy %s (correlation_id=%s)",
                request.policy_id,
                request.correlation_id,
            )

            completed = handle_process_requested(request)
            await publish_completed(exchange, completed)

            logger.info(
                "Completed policy %s (correlation_id=%s)", request.policy_id, request.correlation_id
            )
    except Exception:
        # process() has already rejected the message; keep consuming.
        logger.exception("Failed to process message %s", message.message_id)


async def run() -> None:
    connection = await connect(load_settings())
    async with connection:
        channel = await connection.channel()
        await channel.set_qos(prefetch_count=1)
        exchange, queue = await declare_topology(channel)

        logger.info("Waiting for messages on %s", queue.name)
        async with queue.iterator() as messages:
            async for message in messages:
                await on_message(message, exchange)


def main() -> None:
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s %(message)s")
    try:
        asyncio.run(run())
    except KeyboardInterrupt:
        pass


if __name__ == "__main__":
    main()
