from aio_pika import ExchangeType
from aio_pika.abc import AbstractChannel, AbstractExchange, AbstractQueue

EXCHANGE = "benefits.events"
PROCESSING_QUEUE = "policy.processing"
PROCESSED_QUEUE = "policy.processed"
FAILED_QUEUE = "policy.failed"
PROCESS_REQUESTED = "policy.process.requested"
PROCESS_COMPLETED = "policy.process.completed"
PROCESS_FAILED = "policy.process.failed"


async def declare_topology(channel: AbstractChannel) -> tuple[AbstractExchange, AbstractQueue]:
    exchange = await channel.declare_exchange(EXCHANGE, ExchangeType.TOPIC, durable=True)

    processing = await channel.declare_queue(PROCESSING_QUEUE, durable=True)
    await processing.bind(exchange, routing_key=PROCESS_REQUESTED)

    # Result queues are also declared by the .NET consumer; declaring them here means results
    # published before that consumer has ever started are queued rather than dropped as unroutable.
    processed = await channel.declare_queue(PROCESSED_QUEUE, durable=True)
    await processed.bind(exchange, routing_key=PROCESS_COMPLETED)

    failed = await channel.declare_queue(FAILED_QUEUE, durable=True)
    await failed.bind(exchange, routing_key=PROCESS_FAILED)

    return exchange, processing
