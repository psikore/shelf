import pytest
import grpc
from server.proto_gen import echo_pb2


@pytest.mark.asyncio(loop_scope="session")
async def test_echo_correct_params(echo_stub, param_state):
    """Echo with valid params returns the same message."""
    enc, val = await param_state.get()
    resp = await echo_stub.Echo(
        echo_pb2.CommandRequest(
            encryption_algorithm=enc,
            validation_algorithm=val,
            message="hello world",
        )
    )
    assert resp.message == "hello world"


@pytest.mark.asyncio(loop_scope="session")
async def test_echo_wrong_params_returns_error(echo_stub):
    """Echo with invalid params returns INVALID_ARGUMENT."""
    with pytest.raises(grpc.aio.AioRpcError) as exc_info:
        await echo_stub.Echo(
            echo_pb2.CommandRequest(
                encryption_algorithm="WRONG",
                validation_algorithm="WRONG",
                message="hello",
            )
        )
    assert exc_info.value.code() == grpc.StatusCode.INVALID_ARGUMENT


@pytest.mark.asyncio(loop_scope="session")
async def test_echo_empty_message(echo_stub, param_state):
    """Echo with valid params but empty message returns empty string."""
    enc, val = await param_state.get()
    resp = await echo_stub.Echo(
        echo_pb2.CommandRequest(
            encryption_algorithm=enc,
            validation_algorithm=val,
            message="",
        )
    )
    assert resp.message == ""


@pytest.mark.asyncio(loop_scope="session")
async def test_echo_long_message(echo_stub, param_state):
    """Echo preserves a long message."""
    enc, val = await param_state.get()
    long_msg = "x" * 10000
    resp = await echo_stub.Echo(
        echo_pb2.CommandRequest(
            encryption_algorithm=enc,
            validation_algorithm=val,
            message=long_msg,
        )
    )
    assert resp.message == long_msg
