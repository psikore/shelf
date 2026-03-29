import grpc

from server.proto_gen import echo_pb2
from server.proto_gen import echo_pb2_grpc
from server.params import ParamState


class EchoCommandServicer(echo_pb2_grpc.EchoCommandServiceServicer):
    def __init__(self, param_state: ParamState):
        self._param_state = param_state

    async def Echo(self, request: echo_pb2.CommandRequest, context: grpc.aio.ServicerContext):
        match = await self._param_state.check(
            request.encryption_algorithm,
            request.validation_algorithm,
        )
        if not match:
            await context.abort(
                grpc.StatusCode.INVALID_ARGUMENT,
                "Invalid encryption or validation algorithm parameters.",
            )
        return echo_pb2.CommandResponse(message=request.message)
