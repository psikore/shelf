

# How does the brain know which peers have which pieces?

## Strategy 1: Peers explicitly announce their capabilities (piece maps)

Each peer reports:
* "I have pieces 0-10, 20-30, 50-60"
* or "I have the whole file"
* or "I have none yet"

The brain stores:
peer_id -> piece_map

Then the brain chooses:
* fastest peer
* least loaded
* most complete
* or random for for load balancing

How peers announce:
* On startup
* On file registration
* On piece completion
* On periodic heartbeat

## Strategy 2: The brain probes peers dynamically (lazy discovery)

Instead of peers announcing anything, the brain simply tries to fetch a piece:

The brain builds a dynamic capability map:

## Strategy 3: Peers register "file providers" (metadata-driven)

e.g. regsitration:

```json
{
    peer_id: "peer-123",
    files: ["fileA", "fileB"],
    capabilities: {
        fileA: {range: true, max_rate: 20MB/s},
        fileB: {range: true, cached: false}
    }
}
```
the brain stores 
file_id -> list of peers that can serve it

```
                   +----------------------+
                   |     Brain Service    |
                   |  (piece scheduler)   |
                   +----------+-----------+
                              |
                              | piece_index → offset/length
                              v
        +---------------------+---------------------+
        |                     |                     |
+-------+-------+     +-------+-------+     +-------+-------+
|   Peer A      |     |   Peer B     |      |   Peer C      |
| file provider |     | file provider |     | file provider |
| get/put chunk |     | get/put chunk |     | get/put chunk |
+---------------+     +---------------+     +---------------+

```

# 1. Mental model 

* Brain: global coordinator, owns what needs to be transferred and how it's sliced into pieces.
* Peers: human-driven workers that say "I can get that file" and then execute chunk GET/PUT operations when asked.
* File: state is represented in the brain as a job: piece size, piece hashes, total length, progress.

So the flow is:

Human says: "I can get file X" -> peer registers that with the brain -> brain decides which peer pulls which pieces.

# 2. Peer announcing capability 

When a human says "I have access to that file and I want to get it", the peer sends a capability announcement to the brain:

e.g.
REGISTER_FILE_CAPABILTIY
{
    peer_id: "a1",
    file_id: "myfile on myhost",
    estimated_size, latency_hint, bandwidth_hint
}

Brain stores:

file_id -> { peers: [peer1, peer2, ...]}
peer_id -> { files: [file_id_1, file_id_2, ...]}

At this point the brain knows who could potentially serve the file but not yet which pieces they actually have.

Example end‑to‑end flow
Brain creates job for file_id = F.

Peer A driver: “I can get F” → REGISTER_FILE_CAPABILITY(F, A).

Peer B driver: “I can also get F” → REGISTER_FILE_CAPABILITY(F, B).

Brain splits F into 300 pieces.

Brain assigns:

A: pieces 0–149

B: pieces 150–299

A and B each:

Fetch assigned chunks from wherever they have access.

Call UPLOAD_PIECE(F, piece_index, bytes) back to the brain.

Brain verifies, tracks progress, and when all pieces are complete, marks F as assembled.


# Updated Flow

## Step 1 - Peer A says: "I want file F"
```
RequestFile(F, A)
```
This means:
* "I want to retrieve this file."
* "I have access to it."
* "I am willing to fetch pieces if added."

## Step 2 - Brain checks if a job already exists

Two cases:

### Case 1: No job exists yet
Brain creates a new job:
```
job_id = create_job(file_id = F)
```
The job includes:
* piece size
* number of pieces
* piece hashes (if known)
* empty piece map
* list of participating peers

### Case 2: Job already exists
Brain simply adds Peer A to the job's peer list.

## Step 3 - Peer B also says: "I want file F"
REQUEST_FILE(F, B)
Brain updates:
job[F].peers = [A, B]
Now the brain knows:
* multiple peers want the file
* multiple peers can potentially fetch pieces
* it can distribute work across peers

## Step 4 - Brain assigned pieces to peers
The brian decides:
* which peer fetches which pieces
* how to balance load
* how to maximize throughput
* how to avoid duplication

e.g.
A -> pieces 0-149
B -> pieces 150-299

or

A -> even pieces
B -> odd pieces

## Step 5 - Peers fetch pieces from whatever they have access
Each peer performs:
bytes = peer.get_chunk(offset, length)
This could be:
* a remote host
* a local disk
* a network share
* etc

## Step 6 - Peers upload pieces back to the brain
Peers send:
UPLOAD_PIECE(F, piece_index, bytes)
Brain:
* verifies hash
* marks piece complete
* updates progress
* assigns next piece

## Step 7 - Brain assembles the file when all pieces are complete
Once all pieces are verified:
job[F].status = COMPLETE
Peers can then:
* download the assembled file
* or stream it
* or pass it to another system

## Key Insight

The brain never initiates file transfers.
It only orchestrates them once peers express interest.
This keeps the system:
* reactive
* scalable
* human-driven
* simple to reason about



