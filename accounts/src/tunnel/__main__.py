import asyncio
import aiohttp
import grpc

import tunnel_pb2, tunnel_pb2_grpc


HTTP_POST_URL = "https://example.com/up"
HTTP_GET_URL = "https://example.com/down"
POLL_TIMEOUT = 30 # seconds


class ConnectionState:
    def __init__(self, conn_id, dest_host=None, dest_port=None):
        self.conn_id = conn_id
        self.dest_host = dest_host
        self.dest_port = dest_port
        self.open = True


class TunnelService(tunnel_pb2_grpc.TunnlServicer):
    def __init__(self):
        self._connections = {}  # conn_id -> ConnectionState
        self._next_conn_id = 1
        self._lock = asyncio.Lock() # protect conn_id allocation

    async def _allocate_conn_id(self):
        async with self._lock:
            cid = self._next_conn_id
            self._next_conn_id += 1
            return cid

    async def Stream(self, request_iterator, context):
        session = aiohttp.ClientSession()
        cancel_event = asyncio.Event()

        async def pump_outbound():
            """Forward gRPC -> HTTP POST"""
            try:
                async for frame in request_iterator:
                    if frame.type == FrameType.OPEN:
                        # create connection state
                        cid = frame.conn_id
                        self._connections[cid] = ConnectionState(cid)
                        print(f"[OPEN] conn_id={cid}")

                        # Forward OPEN to HTTP server
                        async with session.post(
                            HTTP_POST_URL,
                            json={"conn_id": cid, "type": "OPEN"}
                        ) as resp:
                            await resp.read()

                    elif frame.type == DATA:
                        cid = frame.conn_id
                        payload = frame.payload
                        if not payload:
                            continue

                        # Forward DATA to HTTP server
                        async with session.post(
                            HTTP_POST_URL,
                            data=payload,
                            params={"conn_id": cid}
                        ) as resp:
                            await resp.read()
                        
                    elif frame.type == CLOSE:
                        cid = frame.conn_id
                        print(f"[CLOSE] conn_id={cid}")
                        # Forward CLOSE to HTTP server
                        async with session.post(
                            HTTP_POST_URL,
                            json={"conn_id": cid, "type": "CLOSE"}
                        ) as resp:
                            await resp.read()

                        self._connections.pop(cid, None)

            except Exception as e:
                print("outbound error: ", e)
                cancel_event.set()

        async def pump_inbound():
            """Long poll HTTP -> gRPC stream"""
            try:
                while not cancel_event.is_set():
                    try:
                        async with session.get(
                            HTTP_GET_URL, 
                            timeout=POLL_TIMEOUT
                        ) as resp:
                            if resp.status != 200:
                                continue

                            msg = await resp.json()
                            # Expecting msgs = [ {conn_id, payload, type}, ...]

                            for msg in msgs:
                                yield tunnel_pb2.TunnelFrame(
                                    conn_id=msg["conn_id"],
                                    payload=msg["payload"].encode(),
                                    type=(
                                        FrameType.OPEN if msg["type"] == "OPEN"
                                        else FrameType.DATA if msg["type"] == "DATA"
                                        else Frametype.CLOSE
                                    )
                                )
                            # If empty or timeout loop again
                    except asyncio.TimeoutError:
                        continue
            except Exception as e:
                print("inbound error: ", e)
                cancel_event.set()

        # Create tasks
        outbound_task = asyncio.create_task(pump_outbound())

        """
        Note: The inbound_generator is not a task.
        The key is that an async function becomes an async generator only
        when it contains a `yield`. And an async generator is not a task - it is
        a pull-driven producer that runs only when you iterate over it.

        That is why pump_inbound() is not started as a task. It doesn't run on its own.
        It runs only when the outer `async for` pulls values from it.

        So the inbound loop is driven by the gRPC server's outbound stream.
        Every time gRPC is ready to send a message, it pulls the next frame from
        the generator.

        The outbound pump however must run idependently and continuously. It consumes
        the gRPC request stream and POSTs data as soon as it arrives. It must run
        concurrently with everything else, whereas the inbound direction (HTTP GET poll -> gRPC) must
        run in lockstep with gRPC's outbound stream.

        i.e.
        The gRPC server expects you to produce outbound messages by yielding them from the RPC handler.
        So the inbound generator is tied directly to the RPC's yield mechanism.

        If you made inbound a task, you'd need to buffer frames somewhere, because tasks cannot
        directly yield into a gRPC stream.

        Think of it this way:
        ```
        gRPC runtimw -> asks for next outbound message
          ->
        async for frame in inbound_generator:
          -> 
        pump_inbound() runs until it hits a yield
          -> 
        frame returned to gRPC
        ```

        The generator suspends after each yield
        It resumes only when gRPC asks for the next message.

        * Outbound (POST) is push-driven (needs task).
        * Inbound (long-poll GET) is pull-driven (generator).
        * gRPC outbound streaming requires yield (generator is perfect for this).
        * Cleanup is centralized in the finally block -> no leaks.
        """
        inbound_generator = pump_inbound()

        try:
            async for frame in inbound_generator:
                yield frame
        finally:
            cancel_event.set()
            outbound_task.cancel()
            await session.close()


async def handle_socks_proxy_connection(reader, writer):
    # SOCKS5 greeting

    data = await reader.readexactly(2)
    nmethods = data[1]
    await reader.readexactly(nmethods) # ignore methods
    writer.write(b"\x05\x00")   # version 5, no auth

    await writer.drain()

    # SOCKS5 connect
    hdr = await reader.readexactly(4)
    atyp = hdr[3]
    if atyp == 1:   # IPv4
        addr = await reader.readexactly(4)
        host = ".".join(str(b) for b in addr)
    elif atyp == 3: # domain
        ln = await reader.readexactly(1)
        host = (await reader.readexactly(ln[0])).decode()
    else:
        writer.close()
        return
    
    port_bytes = await reader.readexactly(2)
    port = int.from_bytes(port_bytes, "big")

    # Reply success
    writer.write(b"\x05\x00\x00\x01\x00\x00\x00\x00")
    await writer.drain()

    # Open gRPC
    channel = grpc_aio.insecure_channel("localhost:50051")
    stub = TunnelStub(channel)
    request_queue = asyncio.Queue()

    async def request_gen():
        while True:
            item = await request_queue.get()
            if item is None:
                return
            yield item

    stream = stub.Stream(request_gen())

    # Send OPEN
    conn_id = id(writer) & 0xFFFFFFFF
    await request_queue.put(TunnelFrame(conn_id=conn_id, type=FrameType.OPEN))

    async def tcp_to_grpc():
        try:
            while True:
                chunk = await reader.read(4096)
                if not chunk:
                    break
                await request_queue.put(
                    TunnelFrame(conn_id=conn_id, payload=chunk, type=FrameType.DATA)
                )
        finally:
            await request_queue.put(
                TunnelFrame(conn_id=conn_id, type=FrameType.CLOSE)
            )
            await request_queue.put(None)

    async def grpc_to_tcp():
        async for frame in stream:
            if frame.conn_id != conn_id:
                continue
            if frame.type == FrameType.DATA:
                writer.write(frame.payload)
                await writer.drain()
            elif frame.type == FrameType.CLOSE:
                break
        writer.close()

    await asyncio.gather(tcp_to_grpc(), grpc_to_tcp())


async def serve():
    server = grpc.aio.server()
    tunnel_pb2_grpc.add_TunnelServicer_to_server(TunnelService(), server)
    server.add_insecure_port("0.0.0.0:50051")
    await server.start()
    print("Tunnel Server listening on 0.0.0.0:50051")

    # SOCKS5 listener
    asyncio.create_task(asyncio.start_server(handle_socks_proxy_connection, "127.0.0.1", 1080))

    """
    In SOCKS terminology:
    * The SOCKS client is the component that applications connect to (curl, SSH, browsers).
    * The SOCKS server is the remote endpoint that actually performs the TCP connecion on behalf of the client.
    But in this architecture:
    * This program is pretending to be the SOCKS server for local applications.
    * But it is actually the client of the real tunnel.

    Local TCP listener (acts like SOCKS server)
      -> 
    SOCKS client logic (parses CONNECT, forwards)
      -> 
    gRPC tunnel client
      -> 
    Tunnel server

    This program must listen on 127.0.0.1:1080 because:
    * Applications like curl, SSH etc expect to connect to a SOCKS proxy.
    * SOCKS proxies always listen for inbound connections.
    So even though it listens like a server, it is still the SOCKS client in the SOCKS protocol sense.
    """
    print("SOCKS5 client listening on 127.0.0.1:1080")

    await server.wait_for_termination()


if __name__ == "__main__":
    asyncio.run(serve())
