import asyncio
import logging
import signal

import grpc.aio

from server.params import ParamState
from server.probe_servicer import ProbeServicer
from server.echo_servicer import EchoCommandServicer
from server.proto_gen import probe_pb2_grpc, echo_pb2_grpc

DEFAULT_PORT = 50051

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)
logger = logging.getLogger("server")


async def serve(port: int = DEFAULT_PORT, rotation_interval: float = 30.0, rotation_stddev: float = 5.0):
    param_state = ParamState(
        rotation_interval=rotation_interval,
        rotation_stddev=rotation_stddev,
    )

    server = grpc.aio.server()
    probe_pb2_grpc.add_ProbeServiceServicer_to_server(ProbeServicer(param_state), server)
    echo_pb2_grpc.add_EchoCommandServiceServicer_to_server(EchoCommandServicer(param_state), server)

    listen_addr = f"[::]:{port}"
    server.add_insecure_port(listen_addr)

    await server.start()
    logger.info("Server listening on %s", listen_addr)

    param_state.start_rotation()

    stop_event = asyncio.Event()

    def _signal_handler():
        logger.info("Shutdown signal received")
        stop_event.set()

    loop = asyncio.get_running_loop()
    for sig in (signal.SIGINT, signal.SIGTERM):
        loop.add_signal_handler(sig, _signal_handler)

    await stop_event.wait()

    param_state.stop_rotation()
    await server.stop(grace=5)
    logger.info("Server stopped")


if __name__ == "__main__":
    asyncio.run(serve())
