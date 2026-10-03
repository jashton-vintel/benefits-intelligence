from aio_pika import ExchangeType
from aio_pika.abc import AbstractChannel, AbstractExchange, AbstractQueue

EXCHANGE = "benefits.events"
PROCESSING_QUEUE = "policy.processing"
PROCESSED_QUEUE = "policy.processed"
PROCESS_REQUESTED = "policy.process.requested"
PROCESS_COMPLETED = "policy.process.completed"


async def declare_topology(channel: AbstractChannel) -> tuple[AbstractExchange, AbstractQueue]:
    exchange = await channel.declare_exchange(EXCHANGE, ExchangeType.TOPIC, durable=True)

    processing = await channel.declare_queue(PROCESSING_QUEUE, durable=True)
    await processing.bind(exchange, routing_key=PROCESS_REQUESTED)

    # Also declared by the .NET consumer; declaring it here means completions published
    # before that consumer has ever started are queued rather than dropped as unroutable.
    processed = await channel.declare_queue(PROCESSED_QUEUE, durable=True)
    await processed.bind(exchange, routing_key=PROCESS_COMPLETED)

    return exchange, processing
