import aiohttp
import grpc
import tunnel_pb2
import tunnel_pb2_grpc


class TunnelService(tunnel_pb2_grpc.TunnelServiceServicer):
    async def StreamTunnel(self, request, context):
        url = f"https://{request.target_host}:{request.target_port}{request.target_path}"

        async with aiohttp.ClientSession() as session:
            async with session.get(url) as resp:
                async for chunk in resp.content.iter_chunked(4096):
                    yield tunnel_pb2.TunnelChunk(
                        data=chunk,
                        end_of_stream=False,
                    )

        yield tunnel_pb2.TunnelChunk(
            data=b"",
            end_of_stream=True
        )
