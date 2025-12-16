import aiohttp
import asyncio
import aio_pika
import json

RABBITMQ_URL = "amqp://guest:guest@localhost/"
QUEUE_NAME = "file_chunks"

async def stream_file_to_rmq(url, chunk_size: int = 128 * 1024)
    connection = await aio_pika.connect_robust(RABBITMQ_URL)
    async with connection:
        channel = await connection.channel()
        await channel.declare_queue(QUEUE_NAME, durable=True)

        async with aiohttp.ClientSession() as session:
            async with session.get(url) as resp:
                if resp.status not in (200, 206):
                    raise Exception(f"Download failed with status {resp.status}")
                
                offset = 0
                async for chunk in resp.content.iter_chunked(chunk_size):
                    message = {
                        "offset": offset,
                        "length": len(chunk),
                        "data": chunk.hex()
                    }
                    await channel.default_exchange.publish(
                        aio_pika.Message(body=json.dumps(message).encode())
                        routing_key=QUEUE_NAME,
                    )
                    print(f"Published chunk {offset}-{offset+len(chunk)-1}")
                    offset += len(chunk)
        
        # Signal EOF
        await channel.default_exchange.publish(
            aio_pika.Message(body=json.dumps({"eof": True}).encode()),
            routing_key=QUEUE_NAME,
        )
        print("published EOF")


async def main():
    url = "http://localhost:8080/"
    await stream_file_to_rmq(url=url)


if __name__ == "__main__":
    asyncio.run(main())
