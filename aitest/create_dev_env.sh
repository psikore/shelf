#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$PROJECT_DIR"

echo "=== Creating development environment for probepoc ==="

# Create virtual environment with uv
echo "Creating virtual environment..."
uv venv .venv --python python3.12 --clear

# Activate virtual environment
echo "Activating virtual environment..."
source .venv/bin/activate

# Install runtime dependencies
echo "Installing runtime dependencies..."
uv pip install \
    grpcio \
    grpcio-tools \
    protobuf \
    python-proto-importer

# Install development/testing dependencies
echo "Installing development dependencies..."
uv pip install \
    pytest \
    pytest-asyncio \
    pytest-timeout \
    honcho

echo ""
echo "=== Development environment created successfully ==="
echo "To activate the virtual environment, run:"
echo "  source .venv/bin/activate"
