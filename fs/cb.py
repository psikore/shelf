from fastapi import FastAPI
from pydantic import BaseModel
import grpc

import file_downloader_pb2 as pb2
import file_downloader_pb2_grpc as pb2_grpc

DOWNLOADER_GRPC_ADDR = "localhost:50051"

app = FastAPI()


class DownloadRequestBody(BaseModel):
    url: str


class DownloadResponseBody(BaseModel):
    file_id: str
    download_url: str


@app.post("/api/download", response_model=DownloadResponseBody)
async def trigger_download(body: DownloadRequestBody):
    # gRPC async client
    async with grpc.aio.insecure_channel(DOWNLOADER_GRPC_ADDR) as channel:
        stub = pb2_grpc.FileDownloaderStub(channel)
        resp = await stub.DownloadAndStore(pb2.DownloadRequest(url=body.url))

    return DownloadResponseBody(
        file_id=resp.file_id,
        download_url=resp.download_url,
    )
