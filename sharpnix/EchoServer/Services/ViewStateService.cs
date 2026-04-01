using Grpc.Core;
using EchoGrpc;
using EchoServer.Helpers;

namespace EchoServer.Services;

public class ViewStateService : ViewState.ViewStateBase
{
    private readonly ILogger<ViewStateService> _logger;
    private readonly PayloadStore _payloadStore;

    public ViewStateService(ILogger<ViewStateService> logger, PayloadStore payloadStore)
    {
        _logger = logger;
        _payloadStore = payloadStore;
    }

    public override Task<ViewStateReply> Generate(ViewStateRequest request, ServerCallContext context)
    {
        try
        {
            // Resolve unsigned payload: either from gadget name (DB lookup) or directly provided
            string unsignedPayload = request.UnsignedPayload;

            if (!string.IsNullOrEmpty(request.Gadget))
            {
                var fromDb = _payloadStore.GetPayload(request.Gadget);
                if (fromDb == null)
                    return Task.FromResult(new ViewStateReply { Error = $"Gadget '{request.Gadget}' not found in database. Run PayloadGenerator first." });
                unsignedPayload = fromDb;
                _logger.LogInformation("Using gadget '{Gadget}' from database", request.Gadget);
            }

            if (string.IsNullOrEmpty(unsignedPayload))
                return Task.FromResult(new ViewStateReply { Error = "Either 'gadget' or 'unsigned_payload' is required" });

            _logger.LogInformation("ViewState generate request: legacy={IsLegacy}, path={Path}, apppath={AppPath}",
                request.IsLegacy, request.TargetPath, request.AppPath);

            string result = ViewStateGenerator.Generate(
                unsignedPayload: unsignedPayload,
                validationKey: request.ValidationKey,
                validationAlg: request.ValidationAlg,
                decryptionKey: request.DecryptionKey,
                decryptionAlg: request.DecryptionAlg,
                targetPath: request.TargetPath,
                appPath: request.AppPath,
                viewStateUserKey: string.IsNullOrEmpty(request.ViewstateUserKey) ? null : request.ViewstateUserKey,
                isLegacy: request.IsLegacy,
                isEncrypted: request.IsEncrypted,
                generator: string.IsNullOrEmpty(request.Generator) ? null : request.Generator);

            return Task.FromResult(new ViewStateReply { Payload = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ViewState generation failed");
            return Task.FromResult(new ViewStateReply { Error = ex.Message });
        }
    }

    public override Task<ListGadgetsReply> ListGadgets(ListGadgetsRequest request, ServerCallContext context)
    {
        try
        {
            if (!_payloadStore.DatabaseExists())
                return Task.FromResult(new ListGadgetsReply { Error = "Payload database not found. Run PayloadGenerator first." });

            var gadgets = _payloadStore.ListGadgets();
            var reply = new ListGadgetsReply();
            foreach (var g in gadgets)
            {
                reply.Gadgets.Add(new EchoGrpc.GadgetInfo
                {
                    Name = g.Name,
                    Description = g.Description,
                    NeedsCommand = g.NeedsCommand
                });
            }
            return Task.FromResult(reply);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListGadgets failed");
            return Task.FromResult(new ListGadgetsReply { Error = ex.Message });
        }
    }
}
