import asyncio
import argparse
import aiohttp
from typing import Optional


class HttpTunnelClient:
    """
    Manages one logical TCP-over-HTTP tunnel to the remote TEP.
    """
    def __init__(
            self,
            session: aiohttp.ClientSession,
            base_url: str,
            dest_host: str,
            dest_port: int,
    ):
        self._session = session
        self._base_url = base_url.rstrip("/")
        self._dest_host = dest_host
        self._dest_port = dest_port
        self._conn_id: Optional[str] = None
        self._closed = False

    async def open(self) -> str:
        """
        Open a logical connection on the remote tep
        POST /open { dest_host, dest_port } -> { id }
        """
        url = f"{self._base_url}/open"
        payload = {
            "dest_host": self._dest_host,
            "dest_port": self._dest_port,
        }
        async with self._session.post(url=url, json=payload) as resp:
            resp.raise_for_status()
            data = await resp.json()
            self._conn_id = data["id"]
            return self._conn_id
        
    async def send_upstream(
            self,
            data: bytes,
            fin: bool = False, 
    ) -> None:
        """
        Send bytes upstream to the tep.
        POST /up/{id}?fin=0|1 (raw body = data)
        """
        if self._conn_id is None:
            raise RuntimeError("Tunnel not opened")
        
        if self._closed:
            return
        
        fin_flag = "1" if fin else "0"
        url = f"{self._base_url}/up/{self._conn_id}?fin={fin_flag}"

        # Fire-and-forget style: we await to propogate errors, but we do not 
        # serialize with downstream. This must not block reading from TCP.
        async with self._session.post(url, data=data) as resp:
            resp.raise_for_status()

        if fin:
            self._closed = True

    async def recv_downstream_once(self) -> Optional[bytes]:
        """
        Receive some bytes from downstream.
        GET /down/{id} -> raw body (may be empty on EOF)
        Returns:
            bytes, or None if remote closed.
        """
        if self._conn_id is None:
            raise RuntimeError("Tunnel not opened")
        
        if self._closed:
            return None
        
        url = f"{self._base_url}/down/{self._conn_id}"
        async with self._session.get(url) as resp:
            if resp.status == 204:
                # No content, treat as EOF
                self._closed = True
                return None
            
            resp.raise_for_status()
            data = await resp.read()
            if not data:
                # Empty body -> treat as EOF
                self._closed = True
                return None
            
            return data


async def handle_client(
    local_reader: asyncio.StreamReader,
    local_writer: asyncio.StreamWriter,
    base_url: str,
    dest_host: str,
    dest_port: int,    
):
    peer = local_writer.get_extra_info("peername")
    print(f"[+] New local connection from {peer}")

    async with aiohttp.ClientSession() as session:
        tunnel = HttpTunnelClient(session, base_url, dest_host, dest_port)
        conn_id = await tunnel.open()
        print(f"[+] Opened remote tep id={conn_id} for {peer}")

        async def upstream():
            """TCP -> HTTP (POST)"""
            try:
                while True:
                    data = await local_reader.read(4096)
                    if not data:
                        # EOF from local (mstsc closed)
                        print(f"[↑] EOF from local {peer}, sending fin upstream")
                        await tunnel.send_upstream(b"", fin=True)
                        break

                    # Send this chunk upstream immediately.
                    await tunnel.send_upstream(data, fin=False)
            except Exception as e:
                print(f"[!] Upstream error for {peer}: {e}")
                # Try to signal fin; ignore errors
                try:
                    await tunnel.send_upstream(b"", fin=True)
                except Exception:
                    pass

        async def downstream():
            """HTTP (GET /down) -> TCP"""
            try:
                while True:
                    chunk = await tunnel.recv_downstream_once()
                    if chunk is None:
                        print(f"[↓] Remote EOF for {peer}")
                        break

                    local_writer.write(chunk)
                    await local_writer.drain()
            except Exception as e:
                print(f"[!] Downstream error for {peer}: {e}")
            finally:
                try:
                    local_writer.close()
                    await local_writer.wait_closed()
                except Exception:
                    pass

        # Run both directions concurrently
        await asyncio.gather(upstream(), downstream())

    print(f"[-] Connection closed for {peer}")


async def main():
    parser = argparse.ArgumentParser(description="TCP-over-http tep")
    parser.add_argument("--listen-host", default="127.0.0.1", help="Local bind host")
    parser.add_argument("--listen-port", type=int, default=13389, help="Local bind port")
    parser.add_argument("--tep-url", required=True, help="Base url of remote tep")
    parser.add_argument("--dest-host", required=True, help="Destination host")
    parser.add_argument("--dest-port", type=int, default=3389, help="Destination port")

    args = parser.parse_args()

    server = await asyncio.start_server( 
        lambda r, w: handle_client(r, w, args.tep_url, args.dest_host, args.dest_port),
        host=args.listen_host,
        port=args.listen_port,
    )

    addr = ", ".join(str(sock.getsockname()) for sock in server.sockets)
    print(f"[*] Listening on {addr}")
    print(f"[*] TEP base URL: {args.tep_url if hasattr(args, 'tep-url') else args.tep_url}")
    print(f"[*] Destination: {args.dest_host}:{args.dest_port}")

    async with server:
        await server.serve_forever()


if __name__ == "__main__":
    asyncio.run(main())
