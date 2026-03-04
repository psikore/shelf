import asyncio
import aiohttp


class ForwardingConnector(aiohttp.TCPConnector):
    """
    Forward http over tcp

    Example Usage:
    connector = ForwardingConnector(via_host="127.0.0.1", via_port=9000, ssl=False)
    async with aiohttp.ClientSession(connector=connector) as session:
        resp = await session.post("https://example.com/api", json={"hello": "world"})
        print(await resp.text())

    """
    def __init__(self, via_host, via_port, **kwargs):
        super().__init__(**kwargs)
        self.via_host = via_host
        self.via_port = via_port

    async def _create_connection(self, req, traces, timeout):
        dest_host = req.url.host
        dest_port = req.url.port

        reader, writer = await asyncio.open_connection(
            self.via_host,
            self.via_port
        )

        header = f"{dest_host}:{dest_port}\n".encode()
        writer.write(header)
        await writer.drain()

        # wrap the reader/writer into aiohttp's transport
        loop = asyncio.get_event_loop()
        transport, protocol = await self._wrap_create_connection(
            loop, timeout, req, reader, writer
        )

        return transport, protocol
    