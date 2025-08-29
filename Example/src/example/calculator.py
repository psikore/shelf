import time
import pytest


def add(a, b):
    return a + b


def subtract(a, b):
    return a - b


def slow_subtract(a, b):
    time.sleep(30)
    return a - b


def multiply(a, b):
    return a * b
