import aiohttp
import asyncio
import pika
import json

RABBITMQ_HOST = "localhost"
QUEUE_NAME = "file_chunks"

async def stream_file_to_rmq(url: str, chunk_size: int = 128 * 1024):
    # rmq connectino (blocking, but fine for producer)
    connection = pika.BlockingConnection(pika.ConnectionParameters(RABBITMQ_HOST))
    channel = connection.channel()
    channel.queue_declare(queue=QUEUE_NAME)

    async with aiohttp.ClientSession() as session:
        async with session.get(url) as resp:
            if resp.status not in (200, 206):
                raise Exception(f"Download failed with status {resp.status}")
            
            offset = 0
            async for chunk in resp.content.iter_chunked(chunk_size):
                # wrap chunk with metadata (offset, length)

                message = {
                    "offset": offset,
                    "length": len(chunk),
                    "data": chunk.hex()     # sent as hex string to keep JSON safe
                }
                channel.basic_publish(
                    exchange="",
                    routing_key=QUEUE_NAME,
                    body=json.dumps(message)
                )
                print(f"Published chunk {offset}-{offset+len(chunk)-1}")
                offset += len(chunk)

            # Signal end of file

            channel.basic_publish(
                exchange="",
                routing_key=QUEUE_NAME,
                body=json.dumps({"eof"; True})
            )
            connection.close()


async def main():
    url = "http://localhost:8080"
    await stream_file_to_rmq(url)


if __name__ == "__main__":
    asyncio.run(main())
