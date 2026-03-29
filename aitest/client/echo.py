import grpc.aio
from client.proto_gen import echo_pb2, echo_pb2_grpc
from client.params_store import load_params


async def run_echo(target: str, message: str, encryption: str | None = None, validation: str | None = None):
    if encryption is None or validation is None:
        params = load_params()
        if params is None:
            print("Error: No saved parameters. Run probe first.")
            return None
        encryption = encryption or params["encryption_algorithm"]
        validation = validation or params["validation_algorithm"]

    async with grpc.aio.insecure_channel(target) as channel:
        stub = echo_pb2_grpc.EchoCommandServiceStub(channel)
        try:
            resp = await stub.Echo(
                echo_pb2.CommandRequest(
                    encryption_algorithm=encryption,
                    validation_algorithm=validation,
                    message=message,
                )
            )
            print(f"Echo response: {resp.message}")
            return resp.message
        except grpc.aio.AioRpcError as e:
            print(f"Error: {e.details()}")
            return None
