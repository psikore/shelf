using Grpc.Core;
using EchoGrpc;

namespace EchoServer.Services;

public class EchoService : Echo.EchoBase
{
    private readonly ILogger<EchoService> _logger;

    public EchoService(ILogger<EchoService> logger)
    {
        _logger = logger;
    }

    public override Task<EchoReply> Send(EchoRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Received: {Message}", request.Message);
        return Task.FromResult(new EchoReply
        {
            Message = request.Message
        });
    }
}
