import os
import shutil
from argparse import ArgumentParser
import pathlib
import ast

from pydecepticon.generators import NameGenerator, NameGeneratorMethod
from pydecepticon.symbolmapping import SymbolMapping
from pydecepticon.transformers.pipelines import TransformerPipeline
from pydecepticon.transformers.renamers import RenameIdentifiers, RenameFunctionAndArgs, RenameClasses
from pydecepticon.transformers.rewriters import ImportRewriter

demo_code_simple = """
class NameDropper:
    def __init__(self, name):
        self.name = name
        
    def drop_name(self):
        return self.name

def greet(name):
    nd = NameDropper(name)
    message = "Hello, " + nd.drop_name()
    if name == nd.drop_name():
        print(message)
    else:
        print("Unknown person")

def main():
    greet("Alice")
    
if __name__ == "__main__":
    main()
"""


def write_transformed_file(
        original_path,
        transformed_code,
        output_dir,
        root_dir,
        module_map: dict[str, str]
):
    original_path = pathlib.Path(original_path)
    root_dir = pathlib.Path(root_dir)
    output_dir = pathlib.Path(output_dir)

    rel_path = original_path.relative_to(root_dir)
    dotted_parts = rel_path.with_suffix("").parts
    original_dotted = ".".join(dotted_parts)

    obfuscated_dotted = module_map.get(original_dotted, original_dotted)
    obfuscated_parts = obfuscated_dotted.split(".")

    if original_path.name in {"__init__.py", "__main__.py"}:
        obfuscated_parts[-1] = original_path.name

    # reconstruct full obfuscated path
    obfuscated_filename = obfuscated_parts[-1] + original_path.suffix if not original_path.name.startswith("__") else original_path.name
    obfuscated_path = output_dir.joinpath(*obfuscated_parts[:-1], obfuscated_filename)

    obfuscated_path.parent.mkdir(parents=True, exist_ok=True)
    obfuscated_path.write_text(transformed_code, encoding="utf-8")

    # original_stem = original_path.stem
    # obfuscated_stem = file_map.get(original_stem, original_stem)
    #
    # obfuscated_filename = obfuscated_stem + original_path.suffix
    # output_path = output_dir / rel_path.parent / obfuscated_filename
    #
    # output_path.parent.mkdir(parents=True, exist_ok=True)
    # output_path.write_text(transformed_code, encoding="utf-8")

def build_module_map(
        py_files,
        source_dir,
        name_generator: NameGenerator,
        mapping: SymbolMapping):
    for file_path in py_files:
        rel_path = file_path.relative_to(source_dir).with_suffix("")
        parts = rel_path.parts
        dotted = ".".join(parts)
        obfuscated_parts = []
        for part in parts:
            if part in {"__init__", "__main__"}:
                obfuscated_parts.append(part)
            else:
                if part not in mapping.module_segment_map:
                    obf = name_generator.generate(name=part)
                    mapping.module_segment_map[part] = obf
                    mapping.mark_used(obf)
                obfuscated_parts.append(mapping.module_segment_map[part])

        obfuscated_dotted = ".".join(obfuscated_parts)
        mapping.module_map[dotted] = obfuscated_dotted


def clear_out_dir(out_dir):
    out_dir = pathlib.Path(out_dir)
    if out_dir.exists() and out_dir.is_dir():
        shutil.rmtree(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)


def collect_py_files(root_dir: pathlib.Path) -> list[pathlib.Path]:
    py_files = list(root_dir.rglob("*.py"))
    if not py_files:
        raise FileNotFoundError(f"No Python files found in {root_dir}")
    return py_files


def build_file_map(
        py_files,
        source_dir,
        name_generator,
        mapping,
):
    for file_path in py_files:
        rel_path = file_path.relative_to(source_dir).with_suffix("")
        parts = rel_path.parts


def main():

    # parse arguments
    parser = ArgumentParser(description="obfuscate a Python file")
    parser.add_argument("source_root_dir", help="Path to the folder containing Python files")
    parser.add_argument("out_dir", help="Output folder")
    parser.add_argument("--erase-out-dir", action="store_true", help="Erase out_dir files beforehand (use with caution!)")

    args = parser.parse_args()

    # prepare the output dir
    source_root_dir = pathlib.Path(args.source_root_dir)
    out_dir = pathlib.Path(args.out_dir)
    if args.erase_out_dir is True:
        clear_out_dir(out_dir=out_dir)
    os.makedirs(out_dir, exist_ok=True)

    if not source_root_dir.is_dir():
        raise NotADirectoryError(f"Not a valid folder: {source_root_dir}")

    # get all source files to obfuscate
    py_files = collect_py_files(root_dir=source_root_dir)

    # name_generator = NameGenerator(
    #     generation_method=NameGeneratorMethod.RANDOM_COMBINATIONS,
    #     character_set="abcdefghijklmnopqrstuvwxyz",
    # )

    # create a name generator
    name_generator = NameGenerator(
        generation_method=NameGeneratorMethod.SUFFIX,
    )

    # create a mapping
    mapping: SymbolMapping = SymbolMapping()

    pipeline = TransformerPipeline(
        name_generator=name_generator,
        mapping=mapping,
    )
    pipeline.add_transformer(ImportRewriter)
    pipeline.add_transformer(RenameClasses)
    pipeline.add_transformer(RenameFunctionAndArgs)
    pipeline.add_transformer(RenameIdentifiers)

    for file_path in py_files:
        with file_path.open("r", encoding="utf-8") as fp:
            source_code = fp.read()
        tree = pipeline.run(source_code)
        obfuscated_source_code: str = ast.unparse(tree)
        write_transformed_file(
            original_path=file_path,
            transformed_code=obfuscated_source_code,
            output_dir=out_dir,
            root_dir=source_root_dir,
            module_map=mapping.module_map,
        )


if __name__ == "__main__":
    main()
