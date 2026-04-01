- I want to create a poc that shows how to write a grpc echo client and server using .NET on Linux/WSL. 

- Create a single solution with separate client and server projects.

Q: Which gRPC NuGet packages are needed?
A: Grpc.Tools, Grpc.Net.Client, Google.Protobuf

Questions to answer?

- How should proto files be structured in the project?

A shared `Protos/` folder at the solution root, referenced by both client and server `.csproj` files. This keeps the contract definition DRY and avoids duplication.

- Can I have a cross-platform .NET application that works both on Linux/WSL and also on Windows?

Yes - I should be able to use .NET 8 or .NET 9 or .NET 10

- Can I generate protobuf bindings on Linux/WSL?

Yes. `Grpc.Tools` works seamlessly on Linux/WSL - it bundles its own `protoc` binary and auto-generates C# bindings during `dotnet build`. No extra tooling needed.

- TLS or insecure for local dev?

Use insecure HTTP/2 for the POC. `dotnet dev-certs https` does not work on WSL. For a local echo demo, skip TLS complexity and use `Http2UnencryptedSupport` on the client side / plain HTTP/2 on the server.

- Does a single published binary work cross-platform (Linux and Windows)?

For framework-dependent (portable) publish: yes, if the .NET runtime is installed on both platforms, the same output runs on both. For self-contained publish: no, you need RID-specific publishes (`linux-x64`, `win-x64`). For a POC, portable is fine.

- Nix dev environment considerations?

Need `dotnet-sdk` in the flake. `Grpc.Tools` bundles its own `protoc` so no extra Nix packages are needed for code generation. For manual testing, `grpcurl` is available in Nixpkgs.

- Which .NET version to target?

.NET 8 (LTS) or .NET 9 both fully support gRPC on Linux. .NET 8 is the safer choice for cross-platform stability.

- Can I use `grpcurl` for manual testing on Linux?

Yes. Add gRPC reflection to the server (`Grpc.AspNetCore.Server.Reflection` package, call `builder.Services.AddGrpcReflection()` and `app.MapGrpcReflectionService()`), then test with `grpcurl -plaintext localhost:5000 list` and `grpcurl -plaintext localhost:5000 echo.Echo/Echo`.

Stretch goals / future considerations:

- Do I want streaming RPCs (server-stream, client-stream, bidirectional) beyond the basic unary echo?
- Should I add gRPC reflection for runtime service discoverability?
- Do I want gRPC health checks and graceful shutdown?
- Any CI considerations? (e.g., GitHub Actions matrix for Linux + Windows builds)
