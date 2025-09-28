import ast

from pydecepticon.generators import NameGenerator
from pydecepticon.symbolmapping import SymbolMapping


class ImportRewriter(ast.NodeTransformer):
    def __init__(
            self,
            name_generator: NameGenerator,
            mapping: SymbolMapping,
    ):
        self.mapping = mapping
        self.name_generator = name_generator

    def visit_Import(self, node):
        for alias in node.names:
            if alias.name in self.mapping.module_map:
                alias.name = self.mapping.module_map[alias.name]
        return node

    def visit_ImportFrom(self, node):
        if node.module in self.mapping.module_map:
            node.module = self.mapping.module_map[node.module]

        for alias in node.names:
            original = alias.name
            if original in self.mapping.class_map:
                alias.name = self.mapping.class_map[original]
            elif original in self.mapping.func_map:
                alias.name = self.mapping.func_map[original]
            elif original in self.mapping.identifier_map:
                alias.name = self.mapping.identifier_map[original]
        return node
