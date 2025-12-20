import aio_pika
import asyncio
import struct
import json
import traceback

RABBITMQ_URL = "amqp://guest:guest@localhost/"
QUEUE_NAME = "file_chunks"

MAX_PARALLEL_HANDLERS = 8


# ---------------------------------------------------------------------------
# Chunk handler task
# ---------------------------------------------------------------------------
async def handle_chunk(offset, data, buffer):
    """
    Store chunk data in a shared buffer keyed by offset.
    """
    try:
        buffer[offset] = data
        print(f"[CONSUMER] Stored chunk at offset {offset} ({len(data)} bytes)")

    except asyncio.CancelledError:
        raise

    except Exception as e:
        print(f"[ERROR] Failed to handle chunk at offset {offset}: {e}")
        traceback.print_exc()
        raise


# ---------------------------------------------------------------------------
# Error message handler
# ---------------------------------------------------------------------------
async def handle_error_message(body):
    try:
        err = json.loads(body.decode())
        print(f"[CONSUMER ERROR] Producer reported error: {err['message']}")
    except Exception:
        print("[CONSUMER ERROR] Malformed error message")


# ---------------------------------------------------------------------------
# Main consumer
# ---------------------------------------------------------------------------
async def consume_and_reassemble():
    connection = await aio_pika.connect_robust(RABBITMQ_URL)

    async with connection:
        channel = await connection.channel()
        queue = await channel.declare_queue(QUEUE_NAME, durable=True)

        buffer = {}  # offset → bytes
        eof_received = False
        semaphore = asyncio.Semaphore(MAX_PARALLEL_HANDLERS)

        async with asyncio.TaskGroup() as tg:

            async for message in queue:
                async with message.process():

                    body = message.body

                    # EOF marker: offset = 2^64 - 1, length = 0
                    if len(body) == 16:
                        offset, length = struct.unpack("!QQ", body)
                        if offset == (2**64 - 1) and length == 0:
                            print("[CONSUMER] EOF received")
                            eof_received = True
                            break

                    # Error message from producer
                    if body.startswith(b"{") and b"type" in body:
                        await handle_error_message(body)
                        continue

                    # Normal chunk
                    try:
                        offset, length = struct.unpack("!QQ", body[:16])
                        chunk = body[16:]

                        if len(chunk) != length:
                            print(f"[WARN] Length mismatch at offset {offset}")
                            continue

                        await semaphore.acquire()

                        tg.create_task(
                            _task_wrapper(
                                semaphore,
                                handle_chunk(offset, chunk, buffer)
                            )
                        )

                    except asyncio.CancelledError:
                        raise

                    except Exception as e:
                        print(f"[ERROR] Failed to parse chunk: {e}")
                        traceback.print_exc()

        # After TaskGroup exits, all chunk handlers are complete
        if eof_received:
            print("[CONSUMER] Reassembling file…")

            ordered_offsets = sorted(buffer.keys())
            assembled = b"".join(buffer[o] for o in ordered_offsets)

            print(f"[CONSUMER] Reassembled file size: {len(assembled)} bytes")
            return assembled

        print("[CONSUMER] EOF not received — incomplete stream")
        return None


# ---------------------------------------------------------------------------
# Wrapper to ensure semaphore release
# ---------------------------------------------------------------------------
async def _task_wrapper(semaphore, coro):
    try:
        await coro
    finally:
        semaphore.release()


# ---------------------------------------------------------------------------
# Entrypoint
# ---------------------------------------------------------------------------
async def main():
    data = await consume_and_reassemble()
    if data is not None:
        print(f"[DONE] Final assembled data length: {len(data)}")


if __name__ == "__main__":
    asyncio.run(main())