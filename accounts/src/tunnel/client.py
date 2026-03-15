import asyncio
import grpc
from grpc import aio
import tunnel_pb2
import tunnel_pb2_grpc

async def main():
    async with aio.insecure_channel("localhost:50051") as channel:
        stub = tunnel_pb2_grpc.TunnelServiceStub(channel)
        req = tunnel_pb2.TunnelRequest(
            target_host="example.com",
            target_port=443,
            http_path="/"
        )
        async for chunk in stub.StreamTunnel(req):
            if chunk.end_of_stream:
                break
            print(chunk.data)

if __name__ == "__main__":
    asyncio.run(main())
