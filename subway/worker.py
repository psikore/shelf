import os
import random
import sys
import time
import logging

# Create log directory if it doesn't exist
logs_dir = "D:\logs"
os.makedirs(logs_dir, exist_ok=True)
log_path = os.path.join(logs_dir, "daemon.log")

# BETTERSTACK_TOKEN = os.getenv("BETTERSTACK_TOKEN")
# BETTERSTACK_URL = "https://in.logs.betterstack.com"
#
#
# class BetterStackHandler(logging.Handler):
#     def emit(self, record):
#         if not BETTERSTACK_TOKEN:
#             return
#
#         log_entry = self.format(record)
#         try:
#             requests.post(
#                 BETTERSTACK_URL,
#                 headers={
#                     "Authorization": f"Bearer {BETTERSTACK_TOKEN}"
#                 },
#                 json={
#                     "message": log_entry
#                 }
#             )
#         except Exception as err:
#             print(f"[ERR] Failed to send log: {err}")
#

# Setup logger
logger = logging.getLogger("worker")
logger.setLevel(logging.INFO)

# File handler
file_handler = logging.FileHandler(log_path)
file_handler.setLevel(logging.INFO)
file_formatter = logging.Formatter("%(asctime)s [%(levelname)s] %(message)s")
file_handler.setFormatter(file_formatter)

# Console handler
console_handler = logging.StreamHandler()
console_handler.setFormatter(logging.Formatter("[%(asctime)s] [PID %(process)d] %(levelname)s: %(message)s"))

# Better Stack handler
# remote_handler = BetterStackHandler()
# remote_handler.setFormatter(logging.Formatter("[%(asctime)s] [PID %(process)d] %(levelname)s: %(message)s"))

logger.addHandler(console_handler)
# logger.addHandler(remote_handler)
logger.addHandler(file_handler)

HEARTBEAT_INTERVAL = 3


def maybe_crash():
    if random.random() < 0.2:   # 20% chance of crashing
        raise RuntimeError(f"[{os.getpid()}] Simulated worker crash")


def variable_sleep():
    return random.uniform(1, 3 * HEARTBEAT_INTERVAL)


def main():
    logger.info("Worker starting up")
    while True:
        try:
            maybe_crash()
            logger.info("heartbeat")
            time.sleep(variable_sleep())
        except Exception as err:
            logger.error(f"Something bad happened: {err}")
            raise

if __name__ == "__main__":
    main()
