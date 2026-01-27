/*
class Module
{
    public async Task ProcessRequest(HttpContext context)
    {
        context.Response.ContentType = "application/octet-stream";
        foreach (var proc in Process.GetProcesses())
        {
            var bytes = SerializeProcessInfo(proc);
            // send response incrementally
            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
            await context.Response.Body.FlushAsync();
        }
    }
}
*/

public class PsStreamHandler : IHttpAsyncHandler
{
    public bool IsReusable => false;

    public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
    {

    }

    public void EndProcssRequest(IAsyncResult result)
    {
        // nothing to do
    }

    public void ProcessRequest(HttpContext context)
    {
        throw new NotSupportedException("Synchronous not supported");
    }

    private async Task HandleRequestAsync(HttpContext context, TaskCompletionSource<object> tcs)
    {
        try
        {
            content.Response.ContentType = "application/octet-stream";
            content.Response.BufferOutput = false;

            var ct = context.Request.TimedOutToken;

            bool continuous = true;     // or read from request

            while (!ct.IsCancellationRequested)
            {
                foreach (var proc in Process.GetProcesses())
                {
                    var info = new ProcessInfo
                    {
                        Pid = proc.Id;
                        Name = proc.ProcessName,
                        CpuPercent = 0,
                        MemoryMb = 0
                    };

                    await WriteMessageAsync(context.Response.OutputStream, info, ct);
                }

                await context.Response.FlushAsync();
                if (!continuous)
                    break;

                await Task.Delay(2000, ct);
            }
            tcs.SetResult(null);
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }        
    }

    private async Task WriteMessageAsync(Stream stream, ProcessInfo msg, CancellationToken ct)
    {
        using (var ms = new MemoryStream())
        {
            msg.WriteTo(ms);
            var length = (int)ms.Length;

            var lengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(length));
            await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length, ct);

            ms.Position = 0;
            await ms.CopyToAsync(stream, 81920, ct);
        }
    }
}

