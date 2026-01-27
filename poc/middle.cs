public class ProcessServiceImpl : ProcessService.ProcessServiceBase
{
    private readonly HttpClient _http;

    public ProcessServiceImpl(HttpClient http)
    {
        _http = http;
    }

    public override async Task StreamProcesses(
        ProcessRequest request,
        IServerStreamWriter<ProcessInfo> responseStream,
        ServerCallContext context)
    {
        var ct = context.CancellationToken;

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://srv.com/ps")
        {
            Content = new StringContent(request.Filter ?? string.Empty, Encoding.UTF8, "text/plain")
        };

        using var httpResponse = await _http.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        httpResponse.EnsureSuccessStatusCode();

        await using var stream = await httpResponse.Content.ReadAsStreamAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            var msg = await.ReadMessageAsync(stream, ct);
            if (msg == null) break; // EOF

            await responseStream.WriteAsync(msg);
        }
    }
}
