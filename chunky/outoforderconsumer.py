import asyncio
import aio_pika
import os
import struct

RABBITMQ_URL = "amqp://guest:guest@localhost/"
QUEUE_NAME = "file_chunks"
OUTPUT_PATH = os.path.join("files", "received_largefile.dat")

# Buffer for out-of-order chunks
chunk_buffer = {}
expected_offset = 0
file_complete = False

async def process_chunk(offset, length, data):
    global expected_offset, chunk_buffer
    # Store chunk if it's not the expected one yet
    if offset != expected_offset:
        chunk_buffer[offset] = data
        print(f"Buffered chunk {offset}-{offset+length-1}")
        return

    # Write expected chunk
    with open(OUTPUT_PATH, "ab") as f:
        f.write(data)
    print(f"Wrote chunk {offset}-{offset+length-1}")
    
    expected_offset += length
    #Flush any buffered chunks that now match expected_offset
    while expected_offset in chunk_buffer:
        buffered_data = chunk_buffer.pop(expected_offset)
        with open(OUTPUT_PATH, "ab") as f:
            f.write(buffered_data)
        print(f"Wrote buffered chunk at {expected_offset}-{expected_offset+len(buffered_data)-1}")
        expected_offset += len(buffered_data)


async def on_message(message: aio_pika.IncomingMessage):
    async with message.process():
        body = message.body
        offset, length = struct.unpack("!QQ", body[:16])
        if offset == 2**64 - 1:
            print("Download complete.")
            return
        
        data = body[16:]
        await process_chunk(offset=offset, length=length, data=data)


async def consume_chunks():
    connection = await aio_pika.connect_robust(RABBITMQ_URL)
    async with connection:
        channel = await connection.channel()
        queue = await channel.declare_queue(QUEUE_NAME, durable=True)
        print("Waiting for chunks...")
        await queue.consume(on_message)
        # Keep consumer alive
        await asyncio.Future()


if __name__ == "__main__":
    asyncio.run(consume_chunks())
