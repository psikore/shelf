#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "=== gRPC Echo POC - Dev Environment Setup ==="

# --- Install .NET SDK if not present ---
if ! command -v dotnet &>/dev/null; then
    echo "Installing .NET 8 SDK..."
    sudo apt-get update -qq
    sudo apt-get install -y -qq dotnet-sdk-8.0
else
    echo ".NET SDK already installed: $(dotnet --version)"
fi

# --- Install hivemind (Procfile process manager) ---
if ! command -v hivemind &>/dev/null; then
    echo "Installing hivemind..."
    curl -sL https://github.com/DarthSim/hivemind/releases/latest/download/hivemind-v1.1.0-linux-amd64.gz \
        | gunzip > /tmp/hivemind
    chmod +x /tmp/hivemind
    sudo mv /tmp/hivemind /usr/local/bin/hivemind
else
    echo "hivemind already installed: $(hivemind --version)"
fi

# --- Install grpcurl for manual testing ---
if ! command -v grpcurl &>/dev/null; then
    echo "Installing grpcurl..."
    sudo apt-get update -qq
    sudo apt-get install -y -qq golang-go
    GOBIN=/usr/local/bin sudo -E go install github.com/fullstorydev/grpcurl/cmd/grpcurl@latest
else
    echo "grpcurl already installed: $(grpcurl --version 2>&1 | head -1)"
fi

# --- Restore NuGet packages ---
echo "Restoring NuGet packages..."
dotnet restore EchoGrpc.sln

# --- Build solution ---
echo "Building solution..."
dotnet build EchoGrpc.sln --no-restore

echo ""
echo "=== Setup complete! ==="
echo ""
echo "To run the server (with hivemind):"
echo "  ./run-dev.sh"
echo ""
echo "To run the client (in another terminal):"
echo "  dotnet run --project EchoClient"
echo "  dotnet run --project EchoClient -- \"Your message here\""
echo ""
echo "To test with grpcurl:"
echo "  grpcurl -plaintext localhost:5000 list"
echo "  grpcurl -plaintext -d '{\"message\": \"hello\"}' localhost:5000 echo.Echo/Send"
