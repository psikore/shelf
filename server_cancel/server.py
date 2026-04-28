#!/usr/bin/env python3
import asyncio
import signal


class LinkServer:
    def __init__(self, host="127.0.0.1", port=9000):
        self.host = host
        self.port = port
        self.server = None
        self.client_tasks = set()
        self.shutdown_event = asyncio.Event()

    # ------------------------------------------------------------
    # Client handler (your TCP forwarding loop goes here)
    # ------------------------------------------------------------
    async def handle_client(self, reader, writer):
        task = asyncio.current_task()
        self.client_tasks.add(task)

        peer = writer.get_extra_info("peername")
        print(f"[client] connected: {peer}")

        try:
            while not self.shutdown_event.is_set():
                data = await reader.read(4096)
                if not data:
                    break

                # Echo back (replace with your forwarder logic)
                writer.write(data)
                await writer.drain()

        except asyncio.CancelledError:
            # Expected during shutdown
            print(f"[client] cancelled: {peer}")
        finally:
            writer.close()
            try:
                await writer.wait_closed()
            except Exception:
                pass

            self.client_tasks.discard(task)
            print(f"[client] closed: {peer}")

    # ------------------------------------------------------------
    # Start server
    # ------------------------------------------------------------
    async def linkstart(self):
        print("[server] starting…")

        self.server = await asyncio.start_server(
            self.handle_client, self.host, self.port
        )

        print(f"[server] listening on {self.host}:{self.port}")

        async with self.server:
            try:
                await self.server.serve_forever()
            except asyncio.CancelledError:
                # Expected when stopping
                print("[server] serve_forever cancelled")

        print("[server] exited serve_forever")

    # ------------------------------------------------------------
    # Stop server gracefully
    # ------------------------------------------------------------
    async def linkstop(self):
        print("[server] stopping…")

        # Stop accepting new connections
        if self.server is not None:
            self.server.close()
            await self.server.wait_closed()
            print("[server] listener closed")

        # Tell handlers to exit their loops
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
# Example main demonstrating start + stop
# ------------------------------------------------------------
async def main():
    link = LinkServer()

    # Start server
    server_task = asyncio.create_task(link.linkstart())

    # Stop on Ctrl+C
    loop = asyncio.get_running_loop()
    stop_event = asyncio.Event()

    def _sigint(*_):
        stop_event.set()

    loop.add_signal_handler(signal.SIGINT, _sigint)

    print("[main] press Ctrl+C to stop")
    await stop_event.wait()

    # Graceful shutdown
    await link.linkstop()

    # Cancel the serving task
    server_task.cancel()
    await asyncio.gather(server_task, return_exceptions=True)

    print("[main] exited cleanly")


if __name__ == "__main__":
    asyncio.run(main())
