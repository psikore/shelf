#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=== Generating proto bindings ==="

# Activate virtual environment
source "$PROJECT_DIR/.venv/bin/activate"

# Generate into a temporary shared location first
cd "$PROJECT_DIR/protos"
proto-importer build

GEN_DIR="$PROJECT_DIR/_proto_gen"

if [ ! -d "$GEN_DIR" ]; then
    echo "Error: proto-importer did not produce output in $GEN_DIR"
    exit 1
fi

# Copy generated bindings into server and client packages
for target in server client; do
    TARGET_DIR="$PROJECT_DIR/$target/proto_gen"
    rm -rf "$TARGET_DIR"
    cp -r "$GEN_DIR" "$TARGET_DIR"
    echo "Copied bindings to $target/proto_gen/"
done

# Clean up the temporary shared output
rm -rf "$GEN_DIR"

echo "=== Proto generation complete ==="
