import asyncio
import grpc

from brain_pb2 import (
    PeerId,
    FileId,
    RequestFileRequest,
    PlaceResult,
    UploadPieceRequest,
)
import brain_pb2_grpc


async def peer_main():
    peer_id = PeerId(id="A1")
    file_id = FileId(id="F1")

    async with grpc.aio.insecure_channel("localhost:50051") as channel:
        stub = brain_pb2_grpc.BrainServiceStub(channel)

        # 1) Request the file - "I want file `F1`"
        resp = await stub.RequestFile(
            RequestFileRequest(peer=peer_id, file=file_id)
        )

        job_id = resp.job_id
        print("Joined job: ", job_id)

        #2) Open assignment stream
        async def assignment_stream():
            # This generator yields PieceResult(s) back to the brain
            # as we complete assigments.
            # For now, just keep it open and send nothing
            while True:
                # In a real implementation, you'd push results here.
                await asyncio.sleep(3600)

        call = stub.PieceAssignmentStream(assignment_stream())

        async for assignment in call:
            print(
                f"Got assignment: piece {assignment.piece_index}, "
                f"offset={assignment.offset}, length={assignment.length}"
            )

            # 3) Fetch the chunk from wherever this peer has access
            # for POC, just fake some bytes
            
            data = b"x" * assignment.length

            # 4) Upload the piece back to the brain
            upload_resp = await stub.UploadPiece(
                UploadPieceRequest(
                    job_id=assignment.job_id,
                    file=assignment.file,
                    peer_id=peer_id,
                    piece_index=assignment.piece_index,
                    data=data,
                )
            )

            print("UploadPiece assigned: ", upload_resp.accepted)

            # 5) Optionally send a PieceResult on the stream ( if you wire it up)
            # left as a follow-up refinement


if __name__ == "__main__":
    asyncio.run(peer_main())
    