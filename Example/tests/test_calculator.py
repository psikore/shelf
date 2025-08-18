from src.example.calculator import add, subtract, slow_subtract
import pytest


def test_add():
    assert add(2, 3) == 5


def test_subtract():
    assert subtract(5, 2) == 3


@pytest.mark.slow
def test_slow_subtract():
    assert slow_subtract(10, 7) == 3
