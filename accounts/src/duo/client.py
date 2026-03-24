import asyncio
import aiohttp
import uuid

SERVER_URL = "http://127.0.0.1:8080/tunfun"
LISTEN_HOST = "127.0.0.1"
LISTEN_PORT = 3390
CHUNK_SIZE = 16 * 1024

async def handle_connection(reader: asyncio.StreamReader, writer: asyncio.StreamWriter):
    conn_id = str(uuid.uuid4())
    session = aiohttp.ClientSession()
    peer = writer.get_extra_info("peername")
    print(f"[client] new conn {conn_id} from {peer}")

    async def upstream():
        try:
            while True:
                data = await reader.read(CHUNK_SIZE)
                if not data:
                    # EOF from local connection -> tell server to close

                    async with session.post(
                        SERVER_URL,
                        data=b"",
                        headers={
                            "X-Conn-ID": conn_id,
                            "X-Close": "1",
                        },                        
                    ) as resp:
                        await resp.read()
                    break

                async with session.post(
                    SERVER_URL,
                    data=data,
                    headers={
                        "X-Conn-ID": conn_id,
                    }
                ) as resp:
                    # Optionally check status
                    await resp.read()
        except Exception as e:
            print(f"[client:{conn_id}] upstream err: {e}")
    
    async def downstream():
        try:
            while True:
                async with session.get(
                    SERVER_URL,
                    headers={
                        "X-Conn-ID": conn_id,
                    },
                    timeout=None,
                ) as resp:
                    closed = resp.headers.get("X-Closed") == "1"
                    body = await resp.read()
                    if body:
                        writer.write(body)
                        await writer.drain()

                    if closed:
                        break

        except Exception as e:
            print(f"[client:{conn_id}] downstream error: {e}")
        finally:
            writer.close()
            await writer.wait_closed()
            await session.close()
    
    await asyncio.gather(upstream(), downstream())
    print(f"[client] conn {conn_id} done")

async def main():
    server = await asyncio.start_server(handle_connection, LISTEN_HOST, LISTEN_PORT)
    print(f"[client] listening on {LISTEN_HOST}:{LISTEN_PORT}")
    async with server:
        await server.serve_forever()

if __name__ == "__main__":
    asyncio.run(main())
