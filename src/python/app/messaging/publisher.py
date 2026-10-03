from aio_pika import DeliveryMode, Message
from aio_pika.abc import AbstractExchange

from app.messaging.topology import PROCESS_COMPLETED
from app.models.messages import ProcessPolicyCompleted


async def publish_completed(exchange: AbstractExchange, event: ProcessPolicyCompleted) -> None:
    message = Message(
        body=event.model_dump_json().encode(),
        content_type="application/json",
        delivery_mode=DeliveryMode.PERSISTENT,
        message_id=str(event.message_id),
        correlation_id=str(event.correlation_id),
        type=PROCESS_COMPLETED,
    )
    await exchange.publish(message, routing_key=PROCESS_COMPLETED)
