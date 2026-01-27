import grpc
import process_pb2
import process_pb2_grpc


def stream_ps():
    channel = grpc.insecure_channel("middle-svc:50051")
    stub = process_pb2_grpc.ProcessServiceStub(channel)
    request = process_pb2.ProcessRequest(filter="", continuous=True)
    try:
        for proc in stub.StreamProcesses(request):
            # update UI
            print(f"{proc.pid} {proc.name}")
    except grpc.RpcError as e:
        print("Stream err: ", e.code(), e.details())            


if __name__ == "__main__":
    stream_processes()
