import asyncio
import aiohttp
import grpc

import tunnel_pb2, tunnel_pb2_grpc


HTTP_POST_URL = "https://example.com/up"
HTTP_GET_URL = "https://example.com/down"
POLL_TIMEOUT = 30 # seconds


class TunnelService(tunnel_pb2_grpc.TunnlServicer):
    async def Stream(self, request_iterator, context):
        session = aiohttp.ClientSession()
        cancel_event = asyncio.Event()

        async def pump_outbound():
            """Forward gRPC -> HTTP POST"""
            try:
                async for frame in request_iterator:
                    payload = frame.payload
                    if not payload:
                        continue

                    async with session.post(HTTP_POST_URL, data=payload) as resp:
                        await resp.read()   # drain body

            except Exception:
                cancel_event.set()

        async def pump_inbound():
            """Long poll HTTP -> gRPC stream"""
            try:
                while not cancel_event.is_set():
                    try:
                        async with session.get(HTTP_GET_URL, timeout=POLL_TIMEOUT) as resp:
                            if resp.status == 200:
                                data = await resp.read()
                                if data:
                                    yield tunnel_pb2.TunnelFrame(payload=data)
                            # If empty or timeout loop again
                    except asyncio.TimeoutError:
                        continue
            except Exception:
                cancel_event.set()

        # Create tasks
        outbound_task = asyncio.create_task(pump_outbound())

        """
        Note: The inbound_generator is not a task.
        The key is that an async function becomes an async generator only
        when it contains a `yield`. And an async generator is not a task - it is
        a pull-driven producer that runs only when you iterate over it.

        That is why pump_inbound() is not started as a task. It doesn't run on its own.
        It runs only when the outer `async for` pulls values from it.

        So the inbound loop is driven by the gRPC server's outbound stream.
        Every time gRPC is ready to send a message, it pulls the next frame from
        the generator.

        The outbound pump however must run idependently and continuously. It consumes
        the gRPC request stream and POSTs data as soon as it arrives. It must run
        concurrently with everything else, whereas the inbound direction (HTTP GET poll -> gRPC) must
        run in lockstep with gRPC's outbound stream.

        i.e.
        The gRPC server expects you to produce outbound messages by yielding them from the RPC handler.
        So the inbound generator is tied directly to the RPC's yield mechanism.

        If you made inbound a task, you'd need to buffer frames somewhere, because tasks cannot
        directly yield into a gRPC stream.

        Think of it this way:
        ```
        gRPC runtimw -> asks for next outbound message
          ->
        async for frame in inbound_generator:
          -> 
        pump_inbound() runs until it hits a yield
          -> 
        frame returned to gRPC
        ```

        The generator suspends after each yield
        It resumes only when gRPC asks for the next message.

        * Outbound (POST) is push-driven (needs task).
        * Inbound (long-poll GET) is pull-driven (generator).
        * gRPC outbound streaming requires yield (generator is perfect for this).
        * Cleanup is centralized in the finally block -> no leaks.
        """
        inbound_generator = pump_inbound()

        try:
            async for frame in inbound_generator:
                yield frame
        finally:
            cancel_event.set()
            outbound_task.cancel()
            await session.close()


async def serve():
    server = grpc.aio.server()
    tunnel_pb2_grpc.add_TunnelServicer_to_server(TunnelService(), server)
    server.add_insecure_port("0.0.0.0:50051")
    await server.start()
    await server.wait_for_termination()


if __name__ == "__main__":
    asyncio.run(serve())
