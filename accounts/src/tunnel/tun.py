import asyncio
import grpc
from grpc import aio

import tunnel_pb2
import tunnel_pb2_grpc

class TunnelService(tunnel_pb2_grpc.TunnelServiceServicer):
    async def StreamTunnel(self, request, context):
        host = request.target_host
        port = request.target_port
        path = request.http_path or "/"

        reader, writer = await asyncio.open_connection(host, port)
        http_req = (
            f"GET {path} HTTP/1.1\r\n"
            f"Host: {host}\r\n"
            f"Connection: close\r\n"
            f"\r\n"
        ).encode("ascii")

        writer.write(http_req)
        await writer.drain()

        try:
            while True:
                chunk = await reader.read(4096)
                if not chunk:
                    yield tunnel_pb2.TunnelChunk(data=b"", end_of_stream=True)
                    break

                yield tunnel_pb2.TunnelChunk(data=chunk, end_of_stream=False)
        finally:
            writer.close()
            await writer.wait_closed()


async def serve():
    server = aio.server()
    tunnel_pb2_grpc.add_TunnelServiceServicer_to_server(
        TunnelService(), server
    )
    server.add_insecure_port("[::]50051")
    await server.start()
    await server.wait_for_termination()

if __name__ == "__main__":
    asyncio.run(main())
