using EchoServer.Services;
using EchoServer.Helpers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

// Register PayloadStore — look for payloads.db in solution root
var dbPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "payloads.db"));
builder.Services.AddSingleton(new PayloadStore(dbPath));

var app = builder.Build();

app.MapGrpcService<EchoService>();
app.MapGrpcService<ViewStateService>();
app.MapGrpcReflectionService();

app.Run();
