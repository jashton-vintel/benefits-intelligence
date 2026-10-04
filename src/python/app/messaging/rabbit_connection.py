import asyncio
import logging

import aio_pika
from aio_pika.abc import AbstractRobustConnection

from app.config import Settings

logger = logging.getLogger(__name__)

CONNECT_ATTEMPTS = 8
FIRST_RETRY_DELAY_SECONDS = 1.0
MAX_RETRY_DELAY_SECONDS = 15.0


async def connect(
    settings: Settings,
    attempts: int = CONNECT_ATTEMPTS,
    first_delay: float = FIRST_RETRY_DELAY_SECONDS,
) -> AbstractRobustConnection:
    """Connect to RabbitMQ, retrying while the broker is still starting.

    A robust connection recovers from drops once established, but the first connection is not
    retried by the client, so a broker that is slow to start would otherwise stop the worker.
    """
    attempt = 1
    delay = first_delay
    while True:
        try:
            return await aio_pika.connect_robust(
                host=settings.rabbitmq_host,
                port=settings.rabbitmq_port,
                login=settings.rabbitmq_user,
                password=settings.rabbitmq_password,
            )
        except (ConnectionError, OSError) as error:
            if attempt >= attempts:
                raise
            logger.warning(
                "RabbitMQ not reachable (attempt %d of %d): %s; retrying in %.0fs",
                attempt,
                attempts,
                error,
                delay,
            )
            await asyncio.sleep(delay)
            delay = min(delay * 2, MAX_RETRY_DELAY_SECONDS)
            attempt += 1
