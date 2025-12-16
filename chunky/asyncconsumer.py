import asyncio
import aio_pika
import json
import os

RABBITMQ_URL = "amqp://guest:guest@localhost/"
QUEUE_NAME = "file_chunks"
OUTPUT_PATH = os.path.join("files", "received_largefile.dat")


async def on_message(message: aio_pika.IncomingMessage):
    async with message.process():
        msg = json.loads(message.body.decode())
        if "eof" in msg:
            print("Download complete.")
            return
        
        offset = msg["offset"]
        data = bytes.fromhex(msg["data"])

        # Append chunk directly to file
        with open(OUTPUT_PATH, "ab") as f:
            f.write(data)

        print(f"Received chunk {offset}-{offset+len(data)-1}")


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
