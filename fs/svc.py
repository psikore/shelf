import asyncio
import os
import uuid
from pathlib import Path

import grpc
import httpx
from fastapi import FastAPI, HTTPException
from fastapi.responses import FileResponse
import uvicorn

import file_downloader_pb2 as pb2
import file_downloader_pb2_grpc as pb2_grpc

DOWNLOAD_DIR = Path("./downloads")
DOWNLOAD_DIR.mkdir(parents=True, exist_ok=True)

HTTP_HOST = "localhost"
HTTP_PORT = 8000
GRPC_PORT = 50051


class FileDownloaderServicer(pb2_grpc.FileDownloaderServicer):
    async def DownloadAndStore(self, request, context):
        url = request.url

        file_id = str(uuid.uuid4())
        file_path = DOWNLOAD_DIR / file_id

        # Simple HTTP download (streaming)
        async with httpx.AsyncClient() as client:
            try:
                async with client.stream("GET", url) as resp:
                    resp.raise_for_status()
                    with file_path.open("wb") as f:
                        async for chunk in resp.aiter_bytes():
                            f.write(chunk)
            except Exception as e:
                await context.abort(grpc.StatusCode.INTERNAL, str(e))

        download_url = f"http://{HTTP_HOST}:{HTTP_PORT}/files/{file_id}"
        return pb2.DownloadReply(file_id=file_id, download_url=download_url)


# ---------- FastAPI app for serving files ----------

app = FastAPI()


@app.get("/files/{file_id}")
async def get_file(file_id: str):
    file_path = DOWNLOAD_DIR / file_id
    if not file_path.exists():
        raise HTTPException(status_code=404, detail="File not found")

    # You can add content-type detection if you want
    return FileResponse(
        path=str(file_path),
        filename=file_id,
        media_type="application/octet-stream",
    )


# ---------- Run gRPC and FastAPI together ----------

async def serve_grpc():
    server = grpc.aio.server()
    pb2_grpc.add_FileDownloaderServicer_to_server(
        FileDownloaderServicer(), server
    )
    server.add_insecure_port(f"[::]:{GRPC_PORT}")
    await server.start()
    await server.wait_for_termination()


async def main():
    # Run FastAPI (uvicorn) and gRPC in the same event loop
    config = uvicorn.Config(app, host=HTTP_HOST, port=HTTP_PORT, loop="asyncio")
    server = uvicorn.Server(config)

    await asyncio.gather(
        server.serve(),
        serve_grpc(),
    )


if __name__ == "__main__":
    asyncio.run(main())
