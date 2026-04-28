# Performance Issues

* 1 POST per tiny RDP packet (often 50-300 bytes)
* 1 GET long-poll that returns only one chunk
* No batching
* No pipelining
* No concurrency
* No persistent streaming

RDP is extremely chatty, and your current design forces a full HTTP round-trip for nearly every tiny packet.

Let's break down the real bottlenecks and fixes that move the needle.

## 1. You POST every tiny packet individually

RDP sends hundreds of tiny packets per second.

Each one becomes

```
TCP -> asyncio -> POST -> HTTP headers -> server -> response -> read -> next packet
```
Even on localhost, this is *milliseconds per packet*, which kills throughput.

## 2. Your GET loop returns only one chunk per request

Your server likely does:

```
read 1 TCP chunk -> respond -> close GET
```

So your client:
* GETs
* waits
* receives one chunk
* GETs again
* waits
* receives one chunk
* repeat forever

## 3. aiohttp session is not reused efficiently

You create a session per connection, which is fine, but:
* You don't reuse connections aggressively
* You don't pipeline
* You don't keep GET open long enough to batch

## 4. You cannot simply "add concurrent POSTs"

Your server TEP (tunnel endpoint) is almost certainly not thread-safe for concurrent POSTs on the same Conn-ID.

# Fixes

You must batch and pipeline

## Fix #1 - Batch upstream POSTs
Instead of:
POST 200 bytes
POST 150 bytes
POST 300 bytes
you want:
POST 4096 bytes (containing all 4 packets)

So:

* Collect packets in a buffer
* flush when buffer >= 32 KB
* OR flush every 5ms

## Fix #2 - Keep GET open and stream multiple chunks

Your server must change from 
read 1 chunk -> respond -> close

to:
while queue not empty:
    write chunk
flush

This is the single most important optimization.

Your GET handler should:
* wait up to 50 - 200 ms
* accumulate multiple TCP reads
* send them in one HTTP response

## Fix #3 - Use a single POST writer task
You cannot safely do concurrent POSTs unless your server is explicitly designed for it.
Instead:
* upstream loop pushes packets into an asyncio.Queue
* a dedicated POST writer task drains the queue and sends batched POSTs

This preserves ordering and avoids server exceptions.

## Fix #4 - Increase CHUNK_SIZE to 64KB

RDP packets are small but TCP frames are not
Use a larger chunk size of 64KB
This reduces syscalls and improves batching.

## Fix #5 - Disable debug prints

Your current code prints on every POST and GET. This can drop throughput by 90%


