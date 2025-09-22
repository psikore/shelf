import ast


class RenameFunctionAndArgs(ast.NodeTransformer):
    def __init__(self):
        self.func_counter = 0
        self.arg_counter = 0
        self.func_map = {}
        self.arg_map = {}

    def _new_func_name(self):
        self.func_counter += 1
        return f"func_{self.func_counter}"

    def _new_arg_name(self):
        self.arg_counter += 1
        return f"arg_{self.arg_counter}"

    def visit_FunctionDef(self, node):
        # Rename function name
        new_name = self._new_func_name()
        self.func_map[node.name] = new_name
        node.name = new_name

        # Rename args
        for arg in node.args.args:
            new_arg_name = self._new_arg_name()
            self.arg_map[arg.arg] = new_arg_name
            arg.arg = new_arg_name

        return self.generic_visit(node)

    def visit_Name(self, node):
        # Rename arg refs
        if node.id in self.arg_map:
            node.id = self.arg_map[node.id]
        return node

class RenameIdentifiers(ast.NodeTransformer):
    def __init__(self):
        self.mapping = {}
        self.counter = 0

    def _new_name(self):
        self.counter += 1
        return f"var_{self.counter}"

    def visit_Name(self, node):
        if isinstance(node.ctx, ast.Store):
            if node.id not in self.mapping:
                self.mapping[node.id] = self._new_name()
            node.id = self.mapping[node.id]
        elif isinstance(node.ctx, ast.Load) and node.id in self.mapping:
            node.id = self.mapping[node.id]
        return node


def obfuscate(source_code):
    tree = ast.parse(source_code)

    transformers = [
        RenameIdentifiers(),
        RenameFunctionAndArgs(),
    ]

    for transformer in transformers:
        tree = transformer.visit(tree)
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
    tree = obfuscate(demo_code)
    obfuscated_code = ast.unparse(tree)
    print(obfuscated_code)
    # compiled = compile(tree, filename="<ast>", mode="exec")
    #exec(compiled)

if __name__ == "__main__":
    main()
