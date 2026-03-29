import asyncio
import grpc.aio
from client.proto_gen import probe_pb2, probe_pb2_grpc
from client.params_store import save_params

ENCRYPTION_ALGORITHMS = ["AES", "3DES", "DES"]
VALIDATION_ALGORITHMS = [
    "HMACSHA256", "SHA1", "HMACSHA384", "HMACSHA512", "MD5", "AES", "3DES",
]


async def run_probe(target: str, timeout: float = 10.0):
    found_lock = asyncio.Lock()
    found_result: dict | None = None

    async def try_combo(stub, enc: str, val: str):
        nonlocal found_result
        async with found_lock:
            if found_result is not None:
                return
        resp = await stub.Probe(
            probe_pb2.ProbeRequest(encryption_algorithm=enc, validation_algorithm=val)
        )
        if resp.success:
            async with found_lock:
                if found_result is None:
                    found_result = {"encryption_algorithm": enc, "validation_algorithm": val}

    async with grpc.aio.insecure_channel(target) as channel:
        stub = probe_pb2_grpc.ProbeServiceStub(channel)
        tasks = [
            try_combo(stub, enc, val)
            for enc in ENCRYPTION_ALGORITHMS
            for val in VALIDATION_ALGORITHMS
        ]
        try:
            await asyncio.wait_for(asyncio.gather(*tasks), timeout=timeout)
        except asyncio.TimeoutError:
            pass

    if found_result:
        save_params(found_result["encryption_algorithm"], found_result["validation_algorithm"])
        print(f"Probe succeeded: {found_result['encryption_algorithm']}, {found_result['validation_algorithm']}")
        return found_result
    else:
        print("Probe timed out: no valid combination found.")
        return None
