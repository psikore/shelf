import keyword
import builtins

PYTHON_KEYWORDS = set(keyword.kwlist)
PYTHON_BUILTINS = set(dir(builtins))
RESERVED_NAMES = PYTHON_KEYWORDS | PYTHON_BUILTINS

class SymbolMapping:
    def __init__(self):
        self.func_map = {}
        self.class_map = {}
        self.identifier_map = {}
        self.arg_map = {}
        self.method_map = {}
        self.module_map = {}
        self.file_map = {}
        self.package_map = {}

        self.used_names = set()

        """
        whitelisted words. These are not keywords or builtins but still required to avoid
        breaking conventions within Python. For example __init__ is a special method in Python,
        and renaming it can break protocol hooks and object instantiation will fail if it is obfuscated.
        """
        self.whitelist = {
            "self",
            "__name__",
            "__init__",
            "__str__",
            "__repr__",
            "__eq__",
            "__lt__",
            "__len__"
        }

    def is_whitelisted(self, name):
        return name in self.whitelist or name in RESERVED_NAMES

    def is_used(self, name) -> bool:
        return name in self.used_names

    def mark_used(self, name):
        self.used_names.add(name)

    def get_all_maps(self):
        return {
            "functions": self.func_map,
            "classes": self.class_map,
            "identifiers": self.identifier_map,
            "arguments": self.arg_map,
            "methods": self.method_map,
            "modules": self.module_map,
            "packages": self.package_map,
        }
