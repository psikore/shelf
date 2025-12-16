import aiohttp
import asyncio
import aio_pika
import json
import struct

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

                    # Pack offset + length header (16 bytes: 8 for offset, 8 for length)
                    header = struct.pack("!QQ", offset, len(chunk))
                    payload = header + chunk

                    await channel.default_exchange.publish(
                        aio_pika.Message(body=payload)
                        routing_key=QUEUE_NAME,
                    )
                    print(f"Published chunk {offset}-{offset+len(chunk)-1}")
                    offset += len(chunk)
        
        # Signal EOF as marker : offset=-1, length = 0
        header = struct.pack("!QQ", (2**64 - 1), 0)
        await channel.default_exchange.publish(
            aio_pika.Message(body=header),
            routing_key=QUEUE_NAME,
        )
        print("published EOF marker")


async def main():
    url = "http://localhost:8080/"
    await stream_file_to_rmq(url=url)


if __name__ == "__main__":
    asyncio.run(main())
