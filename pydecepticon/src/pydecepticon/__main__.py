from argparse import ArgumentParser
import ast

from pydecepticon.transformers.renamers import RenameIdentifiers, RenameFunctionAndArgs


def obfuscate(source_code: str, transformers: list):
    tree = ast.parse(source_code)
    for t in transformers:
        tree = t.visit(tree)
        ast.fix_missing_locations(tree)
    return tree


demo_code = """
def greet(name):
    message = "Hello, " + name
    if name == "Alice":
        print(message)
    else:
        print("Unknown person")
greet("Alice")
"""


def main():
    transformers: list = [
        RenameIdentifiers(),
        RenameFunctionAndArgs(),
    ]

    tree = obfuscate(demo_code, transformers)
    obfuscated_source_code: str = ast.unparse(tree)
    print(obfuscated_source_code)


if __name__ == "__main__":
    main()
