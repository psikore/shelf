import pytest
import grpc.aio

from server.params import ParamState
from server.probe_servicer import ProbeServicer
from server.echo_servicer import EchoCommandServicer
from server.proto_gen import probe_pb2_grpc, echo_pb2_grpc


@pytest.fixture(scope="session")
async def param_state():
    """ParamState with rotation disabled for predictable tests."""
    return ParamState(rotation_interval=9999, rotation_stddev=0)


@pytest.fixture(scope="session")
async def grpc_server(param_state):
    """Start an in-process async gRPC server on a random port."""
    server = grpc.aio.server()
    probe_pb2_grpc.add_ProbeServiceServicer_to_server(ProbeServicer(param_state), server)
    echo_pb2_grpc.add_EchoCommandServiceServicer_to_server(EchoCommandServicer(param_state), server)
    port = server.add_insecure_port("[::]:0")
    await server.start()
    yield port
    await server.stop(grace=0)


@pytest.fixture(scope="session")
async def grpc_channel(grpc_server):
    """Provide an async gRPC channel connected to the test server."""
    channel = grpc.aio.insecure_channel(f"localhost:{grpc_server}")
    yield channel
    await channel.close()


@pytest.fixture(scope="session")
async def probe_stub(grpc_channel):
    return probe_pb2_grpc.ProbeServiceStub(grpc_channel)


@pytest.fixture(scope="session")
async def echo_stub(grpc_channel):
    return echo_pb2_grpc.EchoCommandServiceStub(grpc_channel)
