import pytest
import os


MARKER_FILE = ".setup_done"


def pytest_configure():
    os.environ["EXTRA_NUMBER"] = "3"


def pytest_collection_modifyitems(items):
    if any("operations" in item.keywords for item in items):
        print("\n[Setup] init operations tests")
        with open(MARKER_FILE, "w") as fp:
            fp.write("setup done")


class GlobalContext:
    def __init__(self, val):
        self._ctx = val

    @property
    def ctx(self):
        return self._ctx


@pytest.fixture(scope="session", autouse=True)
def global_context():
    print("[+] pre-context")
    yield GlobalContext(666)
    print("[+] post-context")
