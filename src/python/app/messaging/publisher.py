from uuid import UUID

from aio_pika import DeliveryMode, Message
from aio_pika.abc import AbstractExchange
from pydantic import BaseModel

from app.messaging.topology import PROCESS_COMPLETED, PROCESS_FAILED
from app.models.messages import ProcessPolicyCompleted, ProcessPolicyFailed


async def publish_completed(exchange: AbstractExchange, event: ProcessPolicyCompleted) -> None:
    await _publish(exchange, event, PROCESS_COMPLETED, event.message_id, event.correlation_id)


async def publish_failed(exchange: AbstractExchange, event: ProcessPolicyFailed) -> None:
    await _publish(exchange, event, PROCESS_FAILED, event.message_id, event.correlation_id)


async def _publish(
    exchange: AbstractExchange,
    event: BaseModel,
    routing_key: str,
    message_id: UUID,
    correlation_id: UUID,
) -> None:
    message = Message(
        body=event.model_dump_json().encode(),
        content_type="application/json",
        delivery_mode=DeliveryMode.PERSISTENT,
        message_id=str(message_id),
        correlation_id=str(correlation_id),
        type=routing_key,
    )
    await exchange.publish(message, routing_key=routing_key)
