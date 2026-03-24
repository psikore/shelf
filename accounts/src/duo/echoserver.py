import asyncio

async def handle(reader, writer):
    addr = writer.get_extra_info("peername")
    print(f"client connected: {addr}")

    try:
        while True:
            data = await reader.read(4096)
            if not data:
                break
            writer.write(data)
            await writer.drain()
    except:
        pass
    finally:
        writer.close()
        await writer.wait_closed()
        print("client disconnected")

async def main():
    server = await asyncio.start_server(handle, "127.0.0.1", 5000)
    print("Echo server running on 127.0.0.1:5000")
    async with server:
        await server.serve_forever()

asyncio.run(main())
