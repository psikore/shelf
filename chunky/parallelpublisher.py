"""
- reads chunks sequentially
- schedules each publish as a task
- limits concurrency (e.g. 8 parallel publishes)
- cancels everything cleanly if the parent is cancelled
- recovers channels inside each task
- sends EOF only after all tasks complete
"""
import aiohttp
import asyncio
import aio_pika
import json
import struct
import traceback

RABBITMQ_URL = "amqp://guest:guest@localhost"
QUEUE_NAME = "file_chunks"

MAX_RETRIES = 5
RETRY_DELAY = 1.0
MAX_PARALLEL_PUBLISH = 8

async def safe_publish(channel, routing_key, message):
    for attempt in range(1, MAX_RETRIES + 1):
        try:
            await channel.default_exchange.publish(message, routing_key)
            return channel
        
        except asyncio.CancelledError:
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


async def send_error(channel, message: str):
    payload = json.dumps({"type": "error", "message": message}).encode()
    msg = aio_pika.Message(body=payload)
    await safe_publish(channel, QUEUE_NAME, msg)


async def send_eof(channel):
    eof_header = struct.pack("!QQ", (2**64 - 1), 0)
    msg = aio_pika.Message(body=eof_header)
    await safe_publish(channel, QUEUE_NAME, msg)


async def publish_chunk_task(channel, offset, chunk):
    try:
        header = struct.pack("!QQ", offset, len(chunk))
        payload = header + chunk
        msg = aio_pika.Message(body=payload)

        await safe_publish(channel, QUEUE_NAME, msg)
        print(f"[TASK] Published chunk {offset}-{offset-len(chunk)-1}")

    except asyncio.CancelledError:
        raise

    except Exception as e:
        err = f"Chunk publish failed at offset {offset}: {e}\n{traceback.format_exc()}"
        print(err)
        await send_error(channel, err)
        raise


async def stream_file_to_rmq(url, chunk_size: int = 128 * 1024):
    connection = await aio_pika.connect_robust(RABBITMQ_URL)

    async with connection:
        channel = await connection.channel()
        await channel.declare_queue(QUEUE_NAME, durable=True)

        semaphore = asyncio.Semaphore(MAX_PARALLEL_PUBLISH)

        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(url) as resp:
                    if resp.status not in (200, 206):
                        raise Exception(f"Download failed with status {resp.status}")
                    
                    offset = 0

                    async with asyncio.TaskGroup() as tg:
                        async for chunk in resp.content.iter_chunked(chunk_size):
                            await semaphore.acquire()

                            tg.create_task(
                                _publish_wrapper(
                                    semaphore,
                                    publish_chunk_task(channel, offset, chunk)
                                )
                            )

                            offset += len(chunk)
        
        except asyncio.CancelledError:
            print("[CANCEL] Streaming cancelled")
            raise
    
        except Exception as e:
            err_text = f"Streaming failed: {e}\n{traceback.format_exc()}"
            print(err_text)
            await send_error(channel, err_text)
            await send_eof(channel)
            return

        await send_eof(channel)
        print("Published EOF marker")


async def _publish_wrapper(semaphore, coro):
    try:
        await coro
    finally:
        semaphore.release()


async def main():
    url = "http://localhost:8080/"
    await stream_file_to_rmq(url)


if __name__ == "__main__":
    asyncio.run(main())
