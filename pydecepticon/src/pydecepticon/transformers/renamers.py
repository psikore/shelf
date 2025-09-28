import ast

from pydecepticon.generators import NameGenerator
from pydecepticon.symbolmapping import SymbolMapping


class RenameIdentifiers(ast.NodeTransformer):
    def __init__(
            self,
            name_generator: NameGenerator,
            mapping: SymbolMapping,
    ):
        self.mapping = mapping
        self.name_generator = name_generator

    def _get_unique_name(self, name=None):
        for _ in range(100):    # max iterations
            candidate = self.name_generator.generate(name=name)
            if not self.mapping.is_used(candidate):
                self.mapping.mark_used(candidate)
                return candidate
        raise RuntimeError("Failed to generate a unique name")

    def visit_Name(self, node):
        if isinstance(node.ctx, ast.Store):
            if node.id not in self.mapping.identifier_map:
                self.mapping.identifier_map[node.id] = self._get_unique_name(name=node.id)
            node.id = self.mapping.identifier_map[node.id]
        elif isinstance(node.ctx, ast.Load) and node.id in self.mapping.identifier_map:
            node.id = self.mapping.identifier_map[node.id]
        return node

    def visit_Attribute(self, node):
        self.generic_visit(node)
        if isinstance(node.ctx, ast.Store):
            # rename attribute if it is being stored (e.g. self.name = ...)
            if node.attr not in self.mapping.identifier_map:
                new_name = self._get_unique_name(name=node.attr)
                self.mapping.identifier_map[node.attr] = new_name
            node.attr = self.mapping.identifier_map[node.attr]
        elif isinstance(node.ctx, ast.Load):
            # rename attribute if it is being loaded (e.g. return self.name)
            if node.attr in self.mapping.identifier_map:
                node.attr = self.mapping.identifier_map[node.attr]
        return node

class RenameFunctionAndArgs(ast.NodeTransformer):
    def __init__(
            self,
            name_generator: NameGenerator,
            mapping: SymbolMapping,
    ):
        self.name_generator = name_generator
        self.mapping = mapping

    def _get_unique_name(self, name=None):
        for _ in range(100):    # max iterations
            candidate = self.name_generator.generate(name=name)
            if not self.mapping.is_used(candidate):
                self.mapping.mark_used(candidate)
                return candidate
        raise RuntimeError("Failed to generate a unique name")

    def visit_FunctionDef(self, node):
        # Only rename top-level functions, not methods
        if not isinstance(getattr(node, 'parent', None), ast.ClassDef):
            if not self.mapping.is_whitelisted(node.name):
                # Rename function name
                if node.name not in self.mapping.func_map:
                    new_name = self._get_unique_name(name=node.name)
                    self.mapping.func_map[node.name] = new_name
                node.name = self.mapping.func_map[node.name]

        # Rename args
        for arg in node.args.args:
            if not self.mapping.is_whitelisted(arg.arg):
                if arg.arg not in self.mapping.arg_map:
                    new_arg_name = self._get_unique_name(name=arg.arg)
                    self.mapping.arg_map[arg.arg] = new_arg_name
                arg.arg = self.mapping.arg_map[arg.arg]

        return self.generic_visit(node)

    def visit_Name(self, node):
        if node.id in self.mapping.arg_map:
            # Rename arg refs
            node.id = self.mapping.arg_map[node.id]
        elif node.id in self.mapping.func_map:
            # rename function call refs
            node.id = self.mapping.func_map[node.id]
        return node

    def visit_Attribute(self, node):
        self.generic_visit(node)
        if node.attr in self.mapping.method_map:
            node.attr = self.mapping.method_map[node.attr]
        return node


class RenameClasses(ast.NodeTransformer):
    def __init__(
            self,
            name_generator: NameGenerator,
            mapping: SymbolMapping,
    ):
        self.name_generator = name_generator
        self.mapping = mapping

    def _get_unique_name(self, name=None):
        for _ in range(100):    # max iterations
            candidate = self.name_generator.generate(name=name)
            if not self.mapping.is_used(candidate):
                self.mapping.mark_used(candidate)
                return candidate
        raise RuntimeError("Failed to generate a unique name")

    def visit_ClassDef(self, node):
        new_name = self._get_unique_name(name=node.name)
        self.mapping.class_map[node.name] = new_name
        node.name = new_name
        return self.generic_visit(node)

    def visit_FunctionDef(self, node):
        if isinstance(getattr(node, 'parent', None), ast.ClassDef):
            if not self.mapping.is_whitelisted(node.name):
                original_name = node.name
                if original_name not in self.mapping.method_map:
                    new_name = self._get_unique_name(name=original_name)
                    self.mapping.method_map[original_name] = new_name
                    node.name = new_name
        return self.generic_visit(node)

    def visit_Name(self, node):
        if node.id in self.mapping.class_map:
            node.id = self.mapping.class_map[node.id]
        return node
