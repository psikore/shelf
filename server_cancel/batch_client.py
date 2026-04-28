#!/usr/bin/env python3
import asyncio
import aiohttp
import uuid
import signal

SERVER_URL = "http://127.0.0.1:8080/tunfun"
LISTEN_HOST = "127.0.0.1"
LISTEN_PORT = 3390

TCP_READ_CHUNK = 64 * 1024

UPSTREAM_BATCH_SIZE = 64 * 1024      # max bytes per POST
UPSTREAM_FLUSH_INTERVAL = 0.005      # seconds (5 ms)


class LinkServer:
    def __init__(self, host=LISTEN_HOST, port=LISTEN_PORT):
        self.host = host
        self.port = port
        self.server = None
        self.client_tasks = set()
        self.shutdown_event = asyncio.Event()

    async def handle_connection(self, reader, writer):
        task = asyncio.current_task()
        self.client_tasks.add(task)

        conn_id = str(uuid.uuid4())
        peer = writer.get_extra_info("peername")
        print(f"[client] new conn {conn_id} from {peer}")

        session = aiohttp.ClientSession()
        upstream_queue: asyncio.Queue[tuple[bytes, bool]] = asyncio.Queue()
        upstream_done = asyncio.Event()

        async def tcp_reader_to_queue():
            try:
                while not self.shutdown_event.is_set():
                    data = await reader.read(TCP_READ_CHUNK)
                    if not data:
                        # EOF from local TCP
                        await upstream_queue.put((b"", True))  # close marker
                        break
                    await upstream_queue.put((data, False))
            except asyncio.CancelledError:
                raise
            finally:
                upstream_done.set()

        async def upstream_poster():
            try:
                closed_sent = False
                while not self.shutdown_event.is_set():
                    try:
                        data, is_close = await upstream_queue.get()
                    except asyncio.CancelledError:
                        raise

                    if is_close:
                        if not closed_sent:
                            # send close marker once
                            async with session.post(
                                SERVER_URL,
                                data=b"",
                                headers={"X-Conn-ID": conn_id, "X-Close": "1"},
                            ) as resp:
                                await resp.read()
                        break

                    # start batch with first chunk
                    batch = bytearray(data)
                    # try to fill up to UPSTREAM_BATCH_SIZE or until flush interval
                    try:
                        while len(batch) < UPSTREAM_BATCH_SIZE:
                            try:
                                next_data, next_close = await asyncio.wait_for(
                                    upstream_queue.get(),
                                    timeout=UPSTREAM_FLUSH_INTERVAL,
                                )
                            except asyncio.TimeoutError:
                                break

                            if next_close:
                                # push close marker back and break; handle in next loop
                                await upstream_queue.put((b"", True))
                                break

                            batch.extend(next_data)
                            if len(batch) >= UPSTREAM_BATCH_SIZE:
                                break
                    except asyncio.CancelledError:
                        raise

                    # send batched POST
                    body = bytes(batch)
                    async with session.post(
                        SERVER_URL,
                        data=body,
                        headers={"X-Conn-ID": conn_id},
                    ) as resp:
                        await resp.read()

            except asyncio.CancelledError:
                raise
            except Exception as e:
                print(f"[client:{conn_id}] upstream error: {e}")

        async def downstream_getter():
            try:
                while not self.shutdown_event.is_set():
                    async with session.get(
                        SERVER_URL,
                        headers={"X-Conn-ID": conn_id},
                        timeout=None,
                    ) as resp:
                        closed = resp.headers.get("X-Closed") == "1"
                        body = await resp.read()

                        if body:
                            writer.write(body)
                            await writer.drain()

                        if closed:
                            break
            except asyncio.CancelledError:
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

        try:
            await asyncio.gather(
                tcp_reader_to_queue(),
                upstream_poster(),
                downstream_getter(),
            )
        except asyncio.CancelledError:
            print(f"[client:{conn_id}] connection cancelled")
        finally:
            self.client_tasks.discard(task)
            print(f"[client] conn {conn_id} done")

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

    async def linkstop(self):
        print("[server] stopping…")

        if self.server:
            self.server.close()
            await self.server.wait_closed()
            print("[server] listener closed")

        self.shutdown_event.set()

        for task in list(self.client_tasks):
            task.cancel()

        if self.client_tasks:
            await asyncio.gather(*self.client_tasks, return_exceptions=True)

        print("[server] all client tasks closed")
        print("[server] shutdown complete")


async def main():
    link = LinkServer()
    server_task = asyncio.create_task(link.linkstart())

    loop = asyncio.get_running_loop()
    stop_event = asyncio.Event()

    def _sigint(*_):
        stop_event.set()

    try:
        loop.add_signal_handler(signal.SIGINT, _sigint)
    except NotImplementedError:
        # Windows without Proactor may not support this; you can ignore
        pass

    print("[main] press Ctrl+C to stop")
    await stop_event.wait()

    await link.linkstop()
    server_task.cancel()
    await asyncio.gather(server_task, return_exceptions=True)
    print("[main] exited cleanly")


if __name__ == "__main__":
    asyncio.run(main())
