# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

gRPC proof-of-concept in Python: a server with rotating encryption/validation parameters, and a client that probes for valid parameters then issues echo commands. Both use asyncio-based gRPC.

## Environment Setup

```bash
bash create_dev_env.sh        # Creates .venv, installs all deps
source .venv/bin/activate      # Activate before any Python commands
```

Uses `uv` for venv and package management. Python 3.12.
- all pip commands should use `uv pip` - e.g. `uv pip install -e .` or `uv pip freeze`

## Proto Compilation

Proto bindings are handled at runtime by `python-proto-importer` — no manual `grpc_tools.protoc` step needed. Proto files live in `protos/`.

- the proto-importer can be run via the command `proto-importer build`.
- the tool parameters should be defined in a pyproject.toml in the protos folder.
- proto bindings should be generated via another bash script called .proto_gen.sh that generates the bindings into both client and server packages into a sub-folder/package called proto_gen. i.e.

.\client\proto_gen\
    __init__.py
    ... bindings
.\server\proto_gen\
    __init__.py
    ... bindings

## Running

```bash
python -m server.main          # Start gRPC server
python -m client.main probe    # Run probe command
python -m client.main echo "hello"  # Run echo command
```

## Testing

```bash
pytest                         # Run all tests
pytest tests/test_probe.py     # Single test file
pytest -k "test_name"          # Single test by name
pytest tests/test_longrunning.py  # Long-running integration test
```

VS Code test explorer is configured for pytest discovery. Debug launch configs in `.vscode/launch.json`.

## Architecture

- **Two separate gRPC services** defined in `protos/probe.proto` and `protos/echo.proto`
- **Server** (`server/`): asyncio gRPC server. `params.py` manages the current valid encryption+validation algorithm pair, rotating them randomly on a timer with gaussian jitter. Both servicers read current params from this shared state (behind a lock).
- **Client** (`client/`): CLI tool using argparse subcommands. Probe brute-forces combinations ordered most-to-least popular using async concurrent requests. `params_store.py` persists valid params to `params.json` for reuse by echo.
- **Valid algorithms**: encryption={DES, 3DES, AES}, validation={SHA1, HMACSHA256, HMACSHA384, HMACSHA512, MD5, 3DES, AES}

## Key Design Decisions

- asyncio-based gRPC on both client and server
- Two separate proto files/services (not one combined service)
- Client probe uses concurrent async requests with a lock to stop once a valid pair is found
- `params.json` at project root is a runtime artifact (gitignore it)
