import os
import random
import sys
import time


HEARTBEAT_INTERVAL = 3


def maybe_crash():
    if random.random() < 0.2:   # 20% chance of crashing
        raise RuntimeError(f"[{os.getpid()}] Simulated worker crash")


def variable_sleep():
    return random.uniform(1, 3 * HEARTBEAT_INTERVAL)


def main():
    while True:
        try:
            maybe_crash()
            print(f"[{os.getpid()}] INFO - heartbeat", flush=True)
            time.sleep(variable_sleep())
        except Exception as err:
            print(f"[{os.getpid()}] ERROR - {err}", file=sys.stderr, flush=True)
            raise

if __name__ == "__main__":
    main()
