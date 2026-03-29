import pytest
from server.proto_gen import probe_pb2
from server.params import ENCRYPTION_ALGORITHMS, VALIDATION_ALGORITHMS


@pytest.mark.asyncio(loop_scope="session")
async def test_probe_correct_params(probe_stub, param_state):
    """Probe with the current valid params returns success=True."""
    enc, val = await param_state.get()
    resp = await probe_stub.Probe(
        probe_pb2.ProbeRequest(encryption_algorithm=enc, validation_algorithm=val)
    )
    assert resp.success is True


@pytest.mark.asyncio(loop_scope="session")
async def test_probe_wrong_encryption(probe_stub, param_state):
    """Probe with wrong encryption returns success=False."""
    enc, val = await param_state.get()
    wrong_enc = next(e for e in ENCRYPTION_ALGORITHMS if e != enc)
    resp = await probe_stub.Probe(
        probe_pb2.ProbeRequest(encryption_algorithm=wrong_enc, validation_algorithm=val)
    )
    assert resp.success is False


@pytest.mark.asyncio(loop_scope="session")
async def test_probe_wrong_validation(probe_stub, param_state):
    """Probe with wrong validation returns success=False."""
    enc, val = await param_state.get()
    wrong_val = next(v for v in VALIDATION_ALGORITHMS if v != val)
    resp = await probe_stub.Probe(
        probe_pb2.ProbeRequest(encryption_algorithm=enc, validation_algorithm=wrong_val)
    )
    assert resp.success is False


@pytest.mark.asyncio(loop_scope="session")
async def test_probe_both_wrong(probe_stub, param_state):
    """Probe with both params wrong returns success=False."""
    enc, val = await param_state.get()
    wrong_enc = next(e for e in ENCRYPTION_ALGORITHMS if e != enc)
    wrong_val = next(v for v in VALIDATION_ALGORITHMS if v != val)
    resp = await probe_stub.Probe(
        probe_pb2.ProbeRequest(encryption_algorithm=wrong_enc, validation_algorithm=wrong_val)
    )
    assert resp.success is False


@pytest.mark.asyncio(loop_scope="session")
async def test_probe_empty_params(probe_stub):
    """Probe with empty strings returns success=False."""
    resp = await probe_stub.Probe(
        probe_pb2.ProbeRequest(encryption_algorithm="", validation_algorithm="")
    )
    assert resp.success is False


@pytest.mark.asyncio(loop_scope="session")
async def test_probe_brute_force_finds_match(probe_stub):
    """Iterating all combinations finds exactly one match."""
    matches = []
    for enc in ENCRYPTION_ALGORITHMS:
        for val in VALIDATION_ALGORITHMS:
            resp = await probe_stub.Probe(
                probe_pb2.ProbeRequest(encryption_algorithm=enc, validation_algorithm=val)
            )
            if resp.success:
                matches.append((enc, val))
    assert len(matches) == 1
