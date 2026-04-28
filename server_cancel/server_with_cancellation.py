#!/usr/bin/env python3
import asyncio
import aiohttp
import uuid
import signal


SERVER_URL = "http://127.0.0.1:8080/tunfun"
LISTEN_HOST = "127.0.0.1"
LISTEN_PORT = 3390
CHUNK_SIZE = 16 * 1024


class LinkServer:
    def __init__(self, host=LISTEN_HOST, port=LISTEN_PORT):
        self.host = host
        self.port = port
        self.server = None
        self.client_tasks = set()
        self.shutdown_event = asyncio.Event()

    # ------------------------------------------------------------
    # Handle one TCP connection (your forwarding logic)
    # ------------------------------------------------------------
    async def handle_connection(self, reader, writer):
        task = asyncio.current_task()
        self.client_tasks.add(task)

        conn_id = str(uuid.uuid4())
        peer = writer.get_extra_info("peername")
        print(f"[client] new conn {conn_id} from {peer}")

        session = aiohttp.ClientSession()

        async def upstream():
            try:
                while not self.shutdown_event.is_set():
                    data = await reader.read(CHUNK_SIZE)
                    if not data:
                        # EOF -> tell server to close
                        print(f"[client:{conn_id}] POST close to server")
                        async with session.post(
                            SERVER_URL,
                            data=b"",
                            headers={"X-Conn-ID": conn_id, "X-Close": "1"},
                        ) as resp:
                            await resp.read()
                        break

                    print(f"[client:{conn_id}] POST {len(data)} bytes")
                    async with session.post(
                        SERVER_URL,
                        data=data,
                        headers={"X-Conn-ID": conn_id},
                    ) as resp:
                        await resp.read()

            except asyncio.CancelledError:
                print(f"[client:{conn_id}] upstream cancelled")
                raise
            except Exception as e:
                print(f"[client:{conn_id}] upstream error: {e}")

        async def downstream():
            try:
                while not self.shutdown_event.is_set():
                    print(f"[client:{conn_id}] GET polling server")
                    async with session.get(
                        SERVER_URL,
                        headers={"X-Conn-ID": conn_id},
                        timeout=None,
                    ) as resp:
                        closed = resp.headers.get("X-Closed") == "1"
                        body = await resp.read()

                        if body:
                            print(f"[client:{conn_id}] writing {len(body)} bytes")
                            writer.write(body)
                            await writer.drain()

                        if closed:
                            print(f"[client:{conn_id}] server closed stream")
                            break

            except asyncio.CancelledError:
                print(f"[client:{conn_id}] downstream cancelled")
                raise
            except Exception as e:
                print(f"[client:{conn_id}] downstream error: {e}")
            finally:
                writer.close()
                try:
                    await writer.wait_closed()
                except Exception:
                    pass

                await session.close()

        # Run both loops until one ends or cancellation happens
        try:
            await asyncio.gather(upstream(), downstream())
        except asyncio.CancelledError:
            # Cancel both loops explicitly
            print(f"[client:{conn_id}] connection cancelled")
            # session closed in downstream finally
        finally:
            self.client_tasks.discard(task)
            print(f"[client] conn {conn_id} done")

    # ------------------------------------------------------------
    # Start server
    # ------------------------------------------------------------
    async def linkstart(self):
        print(f"[server] starting on {self.host}:{self.port}")
        self.server = await asyncio.start_server(
            self.handle_connection, self.host, self.port
        )

        async with self.server:
            try:
                await self.server.serve_forever()
            except asyncio.CancelledError:
                print("[server] serve_forever cancelled")

        print("[server] exited serve_forever")

    # ------------------------------------------------------------
    # Stop server gracefully
    # ------------------------------------------------------------
    async def linkstop(self):
        print("[server] stopping…")

        # Stop accepting new connections
        if self.server:
            self.server.close()
            await self.server.wait_closed()
            print("[server] listener closed")

        # Signal all handlers to exit
        self.shutdown_event.set()

        # Cancel all active client tasks
        for task in list(self.client_tasks):
            task.cancel()

        # Wait for all client tasks to finish
        if self.client_tasks:
            await asyncio.gather(*self.client_tasks, return_exceptions=True)

        print("[server] all client tasks closed")
        print("[server] shutdown complete")


# ------------------------------------------------------------
# Example main with Ctrl+C shutdown
# ------------------------------------------------------------
async def main():
    link = LinkServer()

    server_task = asyncio.create_task(link.linkstart())

    loop = asyncio.get_running_loop()
    stop_event = asyncio.Event()

    def _sigint(*_):
        stop_event.set()

    loop.add_signal_handler(signal.SIGINT, _sigint)

    print("[main] press Ctrl+C to stop")
    await stop_event.wait()

    await link.linkstop()

    server_task.cancel()
    await asyncio.gather(server_task, return_exceptions=True)

    print("[main] exited cleanly")


if __name__ == "__main__":
    asyncio.run(main())
