import json
import os

DEFAULT_PARAMS_FILE = "params.json"


def load_params(path: str = DEFAULT_PARAMS_FILE) -> dict | None:
    if not os.path.exists(path):
        return None
    with open(path) as f:
        return json.load(f)


def save_params(encryption: str, validation: str, path: str = DEFAULT_PARAMS_FILE):
    with open(path, "w") as f:
        json.dump({"encryption_algorithm": encryption, "validation_algorithm": validation}, f, indent=2)
