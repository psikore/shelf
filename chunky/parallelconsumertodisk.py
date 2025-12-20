import aio_pika
import asyncio
import struct
import json
import traceback
from pathlib import Path

RABBITMQ_URL = "amqp://guest:guest@localhost/"
QUEUE_NAME = "file_chunks"

MAX_PARALLEL_HANDLERS = 8
MAX_BUFFER_SIZE = 64  # number of chunks allowed to be buffered


# ---------------------------------------------------------------------------
# Chunk handler: parse and store in buffer
# ---------------------------------------------------------------------------
async def handle_chunk(offset, data, buffer, buffer_event):
    try:
        buffer[offset] = data
        buffer_event.set()  # notify writer that new data arrived
        print(f"[CONSUMER] Buffered chunk at offset {offset}")

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
# Writer task: writes chunks in order to disk
# ---------------------------------------------------------------------------
async def writer_task(output_path, buffer, buffer_event, eof_flag):
    next_offset = 0

    with open(output_path, "wb") as f:
        while True:
            # Wait until we have the next chunk or EOF
            while next_offset not in buffer:
                if eof_flag.is_set():
                    # No more chunks will arrive
                    print("[WRITER] EOF reached, finishing")
                    return
                buffer_event.clear()
                await buffer_event.wait()

            # Write chunk
            chunk = buffer.pop(next_offset)
            f.write(chunk)
            next_offset += len(chunk)

            # Optional: flush periodically for safety
            # f.flush()


# ---------------------------------------------------------------------------
# Main consumer
# ---------------------------------------------------------------------------
async def consume_and_stream_to_disk(output_path: str):
    connection = await aio_pika.connect_robust(RABBITMQ_URL)

    async with connection:
        channel = await connection.channel()
        queue = await channel.declare_queue(QUEUE_NAME, durable=True)

        buffer = {}  # offset → bytes
        buffer_event = asyncio.Event()
        eof_flag = asyncio.Event()

        semaphore = asyncio.Semaphore(MAX_PARALLEL_HANDLERS)

        async with asyncio.TaskGroup() as tg:

            # Start writer task
            tg.create_task(writer_task(output_path, buffer, buffer_event, eof_flag))

            async for message in queue:
                async with message.process():
                    body = message.body

                    # EOF marker
                    if len(body) == 16:
                        offset, length = struct.unpack("!QQ", body)
                        if offset == (2**64 - 1) and length == 0:
                            print("[CONSUMER] EOF received")
                            eof_flag.set()
                            buffer_event.set()
                            break

                    # Error message
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

                        # Backpressure: avoid unbounded buffer growth
                        while len(buffer) >= MAX_BUFFER_SIZE:
                            await asyncio.sleep(0.01)

                        await semaphore.acquire()
                        tg.create_task(
                            _task_wrapper(
                                semaphore,
                                handle_chunk(offset, chunk, buffer, buffer_event)
                            )
                        )

                    except asyncio.CancelledError:
                        raise

                    except Exception as e:
                        print(f"[ERROR] Failed to parse chunk: {e}")
                        traceback.print_exc()

        print(f"[CONSUMER] File written to {output_path}")


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
    await consume_and_stream_to_disk("output.bin")


if __name__ == "__main__":
    asyncio.run(main())