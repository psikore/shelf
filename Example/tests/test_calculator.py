from src.example.calculator import add, subtract, slow_subtract
import pytest

import os

@pytest.mark.operations
def test_add():
    assert add(2, int(os.environ["EXTRA_NUMBER"])) == 5


@pytest.mark.operations
def test_subtract():
    assert subtract(5, 2) == int(os.environ["EXTRA_NUMBER"])


@pytest.mark.slow
def test_slow_subtract():
    assert slow_subtract(10, 7) == int(os.environ["EXTRA_NUMBER"])
