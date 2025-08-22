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


@pytest.fixture(scope="session", autouse=True)
def teardown_operations():
    yield
    if os.path.exists(MARKER_FILE):
        print("\n[Teardown] cleanup post operations tests")
        os.remove(MARKER_FILE)    


# @pytest.fixture(scope="session", autouse=True)
# def setup_operations(request):
#     if "operations" in request.keywords:
#         if not os.path.exists(MARKER_FILE):
#             print("\n[Setup] init operations tests")
#             with open(MARKER_FILE, "w") as fp:
#                 fp.write("setup done")
#         yield
#         print("\n[Teardown] cleanup post operations tests")
#         if os.path.exists(MARKER_FILE):
#             os.remove(MARKER_FILE)
#     else:
#         yield


# scope="module" runs once per test file
# autouse=True applies automatically to all tests in the module
# if you want to run only for tests with operations mark,
# conditionally apply it.
# to run only operations