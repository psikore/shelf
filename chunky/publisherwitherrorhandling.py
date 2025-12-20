import aiohttp
import asyncio
import aio_pika
import json
import struct
import traceback

RABBITMQ_URL = "amqp://guest:guest@localhost/"
QUEUE_NAME = "file_chunks"

MAX_RETRIES = 5
RETRY_DELAY  = 1.0


async def safe_publish(channel, routing_key, message):
    """
    Publish with automatic channel recovery and retry.
    Returns the channel (which may be a new one).
    """
    for attempt in range(1, MAX_RETRIES + 1):
        try:
            await channel.default_exchange.publish(message, routing_key=routing_key)
            return channel

        except asyncio.CancelledError:
            # NEVER swallow cancellation - propogate immediately!
            raise

        except aio_pika.exceptions.ChannelClosed:
            print(f"[WARN] Channel closed on attempt {attempt}, reacquiring...")
            channel = await channel.connection.channel()

        except aio_pika.exceptions.AMQPError as e:
            print(f"[ERROR] AMQP error on attempt {attempt}: {e}")
            await asyncio.sleep(RETRY_DELAY)

        except Exception as e:
            print(f"[ERROR] Unexpected publish error: {e}")
            await asyncio.sleep(RETRY_DELAY)

    raise RuntimeError("Failed to publish after retries")

async def send_error(channel, error_msg: str):
    """
    Send a structured error message to the consumer
    """
    payload = json.dumps({
        "type": "error",
        "message": error_msg
    }).encode()
    msg = aio_pika.Message(body=payload)
    await safe_publish(channel, QUEUE_NAME, msg)


async def send_eof(channel):
    eof_header = struct.pack("!QQ, (2**64 - 1), 0")
    msg = aio_pika.Message(body=eof_header)
    await safe_publish(channel, QUEUE_NAME, msg)


async def stream_file_to_rmq(url, chunk_size: int = 128 * 1024):
    connection = await aio_pika.connect_robust(RABBITMQ_URL)

    async with connection:
        channel = await connection.channel()
        await channel.declare_queue(QUEUE_NAME, durable=True)

        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(url) as resp:
                    if resp.status not in (200, 206):
                        raise Exception(f"Download failed with status {resp.status}")
                    
                    offset = 0

                    async for chunk in resp.content.iter_chunked(chunk_size):
                        try:
                            header = struct.pack("!QQ", offset, len(chunk))
                            payload = header + chunk
                            msg = aio_pika.Message(body=payload)

                            channel = await safe_publish(channel, QUEUE_NAME, msg)

                            print(f"Published chunk {offset}-{offset+len(chunk)-1}")
                            offset += len(chunk)

                        except asyncio.CancelledError:
                            # propogate cancellation immediately
                            raise

                        except Exception as e:
                            # Non-cancellation errors inside the loop
                            err_text = f"Chunk publish failed: {e}\n{traceback.format_exc()}"
                            print(err_text)
                            await send_error(channel, err_text)
                            await send_eof(channel)
                            return

        except asyncio.CancelledError:
            # If the task is cancelled, propogate immediately
            print("[CANCEL] Streaming task cancelled")
            raise
        
        except Exception as e:
            # Any other pipeline-level error
            err_text = f"Streaming failed: {e}\n{traceback.format_exc()}"
            print(err_text)
            # notify consumer of failure
            await send_error(channel, err_text)
            await send_eof(channel)
            return

        # normal EOF
        await send_eof(channel)
        print("Published EOF marker")


async def main():
    url = "http://localhost:8080"
    await stream_file_to_rmq(url)


if __name__ == "__main__":
    asyncio.run(main())
