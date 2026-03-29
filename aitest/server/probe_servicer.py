import grpc

from server.proto_gen import probe_pb2
from server.proto_gen import probe_pb2_grpc
from server.params import ParamState


class ProbeServicer(probe_pb2_grpc.ProbeServiceServicer):
    def __init__(self, param_state: ParamState):
        self._param_state = param_state

    async def Probe(self, request: probe_pb2.ProbeRequest, context: grpc.aio.ServicerContext):
        match = await self._param_state.check(
            request.encryption_algorithm,
            request.validation_algorithm,
        )
        return probe_pb2.ProbeResponse(success=match)
