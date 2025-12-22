import asyncio
import aio_pika
from aio_pika import Message, DeliveryMode, connect_robust, ExchangeType
from aiormq import DeliveryError


class DirectPublisher:
    def __init__(
            self,
            url: str,
            exchange_name: str
    ):
        self._url = url
        self._exchange_name = exchange_name
        self._connection = None
        self._channel = None
        self._exchange = None

    async def connect(self):
        # RobustConnection auto-reconnects
        self._connection = await connect_robust(self._url)

        # enable publisher confirms
        """
        By default on_return_raises=False
        - returned messages do NOT raise exceptions.
        - aio_pika emits an internal callback (`on_return`)
        - You must manually subscribe to it if you want to know about returned messages
        - if you don't, the message is effectively "lost" from your persepctive
        - await publish() always succeeds, even if the message was unroutable
        - you must handle returns manually
        
        If on_return_raises=True:
        - publish() raises DeliveryError for unroutable messages
        """
        self._channel = await self._connection.channel(
            publisher_confirms=True,
            on_return_raises=True,
        )

        def on_return_cb(message: aio_pika.IncomingMessage):
            print("Returned: ", message)

        # declare a durable direct exchange
        self._exchange = await self._channel.declare_exchange(
            self._exchange_name,
            ExchangeType.DIRECT,
            durable=True,
        )

    async def publish(self, routing_key: str, body: bytes):
        """
        Publish a message with:
        - mandatory=True (fail if unroutable)
        - persistent delivery mode
        - publisher confirms enabled
        :param routing_key:
        :param body:
        :return:
        """
        msg = Message(
            body=body,
            delivery_mode=DeliveryMode.PERSISTENT,
        )

        try:
            await self._exchange.publish(
                msg,
                routing_key="missing.key",
                mandatory=True,
            )

            # if we reach here:
            # - message was routable to at least one queue
            # - broker confirmed it (publisher_confirms=True)
            return True

        except DeliveryError as ex:
            # Message was unroutable OR broker NACKed it
            print(f"[UNROUTABLE] routing_key={routing_key} error={ex}")
            return False

        except Exception as ex:
            # Connection closed, channel closed, network issue, etc
            print(f"[PUBLISH ERROR] {ex}")
            return False


    async def close(self):
        if self._connection:
            await self._connection.close()


async def main():
    publisher = DirectPublisher(
        url="amqp://guest:guest@localhost/",
        exchange_name="events.direct",
    )

    await publisher.connect()

    ok = await publisher.publish("user created", b"hello world")

    print("Publish: ", ok)
    await publisher.close()


if __name__ == "__main__":
    asyncio.run(main())
