import aiohttp
import asyncio
import os
import json
import time


PROGRESS_FILE = "download_progress.json"

def load_progress():
    if os.path.exists(PROGRESS_FILE):
        with open(PROGRESS_FILE, "r") as fp:
            return json.load(fp)
        

def save_progress(progress):
    with open(PROGRESS_FILE, "w") as fp:
        json.dump(progress, fp)

        
async def download_file(url: str, output_path: str, chunk_size: int = 128 * 1024, max_retries: int = 5, retry_delay: int = 3):
    os.makedirs(os.path.dirname(output_path), exist_ok=True)

    progress = load_progress()
    downloaded = progress.get(output_path, 0)

    attempt = 0
    while attempt < max_retries:
        try:
            headers = {}
            if downloaded > 0:
                headers["Range"] = f"bytes={downloaded}-"

                async with aiohttp.ClientSession() as session:
                    async with session.get(url, headers=headers, timeout=aiohttp.ClientTimeout(total=None)) as resp:
                        if resp.status not in (200, 206):
                            raise Exception(f"Download failed with status {resp.status}")
                        
                        # Determine total size
                        total_size = None
                        if "Content-Range" in resp.headers:
                            # e.g. "bytes 1000-1999/5000"
                            total_size = int(resp.headers["Content-Range"].split("/")[-1])
                        if "Content-Length" in resp.headers:
                            total_size = int(resp.headers["Content-Length"])

                        # open file for writing in binary mode
                        mode = "ab" if downloaded > 0 else "wb"
                        with open(output_path, mode) as f:
                            async for chunk in resp.content.iter_chunked(chunk_size):
                                f.write(chunk)
                                downloaded += len(chunk)
                                progress[output_path] = downloaded
                                save_progress(progress=progress)
                                print(f"Wrote {len(chunk)} bytes, total {downloaded}")

                if total_size is not None and downloaded >= total_size:
                    progress.pop(output_path, None)
                    save_progress(progress=progress)
                    print(f"Download complete: {output_path}. Progress entry removed")
                return  # success, exit function
        except (aiohttp.ClientError, asyncio.TimeoutError, Exception) as err:
            attempt += 1
            print(f"Error: {err}. Retry {attempt}/{max_retries} in {retry_delay}sec...")
            time.sleep(retry_delay)

    raise Exception(f"Failed to download after {max_retries} retries")


async def main():
    url = "http://localhost:8080"
    output_path = os.path.join("files", "largefile.dat")
    await download_file(url, output_path)


if __name__ == "__main__":
    asyncio.run(main())
