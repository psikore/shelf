import asyncio
import socket


async def pump(loop, src, dst):
    while True:
        data = await loop.sock_recv(src, 4096)
        if not data:
            break

        print(f"sending {len(data)} bytes")
        await loop.sock_sendall(dst, data)
    try: dst.shutdown(socket.SHUT_WR)
    except OSError: pass

async def handle_client(loop, client_sock):
    """
    Accept a client connection and forward it to an upstream server.
    """
    print("accepting connection")
    upstream = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    upstream.setblocking(False)
    print("connecting")
    await loop.sock_connect(upstream, ("127.0.0.1", 9000))

    t1 = asyncio.create_task(pump(loop, client_sock, upstream))
    t2 = asyncio.create_task(pump(loop, upstream, client_sock))

    # Wait for either direction to finish
    await asyncio.wait({t1, t2}, return_when=asyncio.FIRST_COMPLETED)

    print("closing connection")
    client_sock.close()
    upstream.close()

async def main():
    print("forwarder")
    loop = asyncio.get_running_loop()
    server_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server_sock.bind(("127.0.0.1", 8080))
    server_sock.listen()
    server_sock.setblocking(False)

    while True:
        print("binding on 127.0.0.1:8080")
        client_sock, _ = await loop.sock_accept(server_sock)
        client_sock.setblocking(False)
        asyncio.create_task(handle_client(loop, client_sock))

if __name__ == "__main__":
    asyncio.run(main())
