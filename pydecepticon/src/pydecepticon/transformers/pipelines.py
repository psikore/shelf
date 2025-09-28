import ast

from typing import Optional

from pydecepticon.generators import NameGenerator
from pydecepticon.symbolmapping import SymbolMapping


def attach_parents(node, parent=None):
    """ inject parent refs """
    for child in ast.iter_child_nodes(node):
        child.parent = node
        attach_parents(child, node)


class TransformerPipeline:
    def __init__(
            self,
            name_generator: NameGenerator,
            mapping: Optional[SymbolMapping] = None,
    ):
        self.name_generator = name_generator
        if mapping is None:
            self.mapping = SymbolMapping()
        else:
            self.mapping = mapping
        self.transformers = []

    def add_transformer(self, transformer_cls):
        transformer = transformer_cls(
            name_generator=self.name_generator,
            mapping=self.mapping,
        )
        self.transformers.append(transformer)

    def run(self, source_code: str):
        tree = ast.parse(source_code)
        attach_parents(tree)
        for transformer in self.transformers:
            tree = transformer.visit(tree)
            ast.fix_missing_locations(tree)
        return tree
