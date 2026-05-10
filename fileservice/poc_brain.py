import asyncio
import grpc

from brain_pb2 import (
    RequestFileResponse,
    PieceAssignment,
    UploadPieceResponse,
)
import brain_pb2_grpc


class BrainService(brain_pb2_grpc.BrainServiceServicer):
    def __init__(self):
        # Very rough in-memory state
        self.jobs = {}        # job_id -> job state
        self.assign_queues = {}  # peer_id -> asyncio.Queue[PieceAssignment]

    async def RequestFile(self, request, context):
        # Create or join job for file
        job_id = f"job-{request.file.id}"
        if job_id not in self.jobs:
            self.jobs[job_id] = {
                "file_id": request.file.id,
                "peers": set(),
                # TODO: piece map, metadata, etc.
            }
        self.jobs[job_id]["peers"].add(request.peer.id)

        # TODO: initialize piece metadata if first time
        return RequestFileResponse(
            job_id=job_id,
            file_size=0,   # fill in if known
            piece_size=0,  # fill in if known
            num_pieces=0,  # fill in if known
        )

    async def PieceAssignmentStream(self, request_iterator, context):
        # Each stream is tied to a single peer
        # In practice you'd authenticate and derive peer_id from context
        peer_id = "peer-from-auth"
        q = self.assign_queues.setdefault(peer_id, asyncio.Queue())

        async def consume_results():
            async for result in request_iterator:
                # Brain receives PieceResult from peer
                # Update job state, maybe reassign on failure
                print(f"Result from {result.peer.id}: piece {result.piece_index}, success={result.success}")

        asyncio.create_task(consume_results())

        # Stream assignments to this peer
        while True:
            assignment = await q.get()
            yield assignment

    async def UploadPiece(self, request, context):
        # Brain receives actual bytes for a piece
        print(
            f"Received piece {request.piece_index} "
            f"for job {request.job_id} from {request.peer.id}, "
            f"len={len(request.data)}"
        )
        # TODO: verify hash, mark complete, etc.
        return UploadPieceResponse(accepted=True)


async def serve():
    server = grpc.aio.server()
    brain_pb2_grpc.add_BrainServiceServicer_to_server(BrainService(), server)
    server.add_insecure_port("[::]:50051")
    await server.start()
    await server.wait_for_termination()


if __name__ == "__main__":
    asyncio.run(serve())
