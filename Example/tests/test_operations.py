import pytest
from src.example.calculator import multiply

# do not import this as it imports a second copy of the session
# fixture and becomes confusing when the global context gets set twice!

# from conftest import global_context


def test_multiply(global_context):
    val = global_context.ctx
    assert multiply(2, val) == 1332
