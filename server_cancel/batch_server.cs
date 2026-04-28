// File: TepServer.cs
// Compile with: dotnet new console -n TepServer && replace Program.cs with this file content (namespace adjusted) or similar.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class ConnState
{
    public string ConnId { get; }
    public TcpClient TcpClient { get; }
    public NetworkStream Stream { get; }
    public ConcurrentQueue<byte[]> DownQueue { get; } = new ConcurrentQueue<byte[]>();
    public CancellationTokenSource Cts { get; } = new CancellationTokenSource();

    public volatile bool ClosedFromClient = false;
    public volatile bool ClosedFromRemote = false;

    public ConnState(string connId, TcpClient client)
    {
        ConnId = connId;
        TcpClient = client;
        Stream = client.GetStream();
    }
}

class TepServer
{
    private readonly HttpListener _listener = new HttpListener();
    private readonly ConcurrentDictionary<string, ConnState> _conns = new ConcurrentDictionary<string, ConnState>(StringComparer.Ordinal);

    private const string Prefix = "http://+:8080/tunfun/";
    private const string RemoteHost = "127.0.0.1";
    private const int RemotePort = 3389;

    private const int TcpReadChunk = 64 * 1024;
    private const int MaxBatchBytes = 64 * 1024;
    private static readonly TimeSpan MaxBatchWait = TimeSpan.FromMilliseconds(50);

    public TepServer()
    {
        _listener.Prefixes.Add(Prefix);
    }

    public async Task RunAsync(CancellationToken token)
    {
        _listener.Start();
        Console.WriteLine($"[TEP] Listening on {Prefix}");

        try
        {
            while (!token.IsCancellationRequested)
            {
                var ctx = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(() => HandleContextAsync(ctx), token);
            }
        }
        catch (HttpListenerException) when (token.IsCancellationRequested)
        {
            // normal shutdown
        }
        finally
        {
            _listener.Stop();
        }
    }

    private async Task HandleContextAsync(HttpListenerContext ctx)
    {
        try
        {
            var req = ctx.Request;
            var resp = ctx.Response;

            string connId = req.Headers["X-Conn-ID"];
            if (string.IsNullOrEmpty(connId))
            {
                resp.StatusCode = 400;
                byte[] msg = Encoding.UTF8.GetBytes("Missing X-Conn-ID");
                await resp.OutputStream.WriteAsync(msg, 0, msg.Length);
                resp.Close();
                return;
            }

            bool isClose = req.Headers["X-Close"] == "1";

            if (req.HttpMethod == "POST")
            {
                await HandlePostAsync(connId, isClose, req, resp);
            }
            else if (req.HttpMethod == "GET")
            {
                await HandleGetAsync(connId, req, resp);
            }
            else
            {
                resp.StatusCode = 405;
                resp.Close();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[TEP] Error handling request: {e}");
            try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { }
        }
    }

    private ConnState GetOrCreateConn(string connId)
    {
        return _conns.GetOrAdd(connId, id =>
        {
            var client = new TcpClient();
            client.NoDelay = true;
            client.Connect(RemoteHost, RemotePort);
            var state = new ConnState(id, client);
            Console.WriteLine($"[TEP] New conn {id} -> {RemoteHost}:{RemotePort}");

            // Start background reader from remote TCP
            _ = Task.Run(() => RemoteReaderLoopAsync(state));
            return state;
        });
    }

    private async Task RemoteReaderLoopAsync(ConnState state)
    {
        var buffer = new byte[TcpReadChunk];
        try
        {
            while (!state.Cts.IsCancellationRequested)
            {
                int n = await state.Stream.ReadAsync(buffer, 0, buffer.Length, state.Cts.Token).ConfigureAwait(false);
                if (n <= 0)
                    break;

                var chunk = new byte[n];
                Buffer.BlockCopy(buffer, 0, chunk, 0, n);
                state.DownQueue.Enqueue(chunk);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Console.WriteLine($"[TEP:{state.ConnId}] Remote reader error: {e.Message}");
        }
        finally
        {
            state.ClosedFromRemote = true;
            Console.WriteLine($"[TEP:{state.ConnId}] Remote closed");
            CleanupIfDone(state.ConnId, state);
        }
    }

    private async Task HandlePostAsync(string connId, bool isClose, HttpListenerRequest req, HttpListenerResponse resp)
    {
        var state = GetOrCreateConn(connId);

        if (isClose)
        {
            state.ClosedFromClient = true;
            try
            {
                state.TcpClient.Client.Shutdown(SocketShutdown.Send);
            }
            catch { }
            Console.WriteLine($"[TEP:{connId}] Client requested close");
        }
        else
        {
            // Read entire body and write to remote TCP
            using (var ms = new MemoryStream())
            {
                await req.InputStream.CopyToAsync(ms).ConfigureAwait(false);
                var data = ms.ToArray();
                if (data.Length > 0)
                {
                    try
                    {
                        await state.Stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                        await state.Stream.FlushAsync().ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[TEP:{connId}] Error writing to remote: {e.Message}");
                        state.ClosedFromRemote = true;
                    }
                }
            }
        }

        resp.StatusCode = 200;
        resp.ContentLength64 = 0;
        resp.Close();

        CleanupIfDone(connId, state);
    }

    private async Task HandleGetAsync(string connId, HttpListenerRequest req, HttpListenerResponse resp)
    {
        if (!_conns.TryGetValue(connId, out var state))
        {
            // No such connection
            resp.StatusCode = 410; // Gone
            resp.ContentLength64 = 0;
            resp.Close();
            return;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var ms = new MemoryStream();

        while (ms.Length < MaxBatchBytes && sw.Elapsed < MaxBatchWait)
        {
            if (state.DownQueue.TryDequeue(out var chunk))
            {
                ms.Write(chunk, 0, chunk.Length);
                if (ms.Length >= MaxBatchBytes)
                    break;
            }
            else
            {
                // no data yet; small sleep to avoid busy spin
                await Task.Delay(5).ConfigureAwait(false);
            }
        }

        byte[] body = ms.ToArray();
        bool closed = state.ClosedFromRemote && state.DownQueue.IsEmpty;

        if (closed)
        {
            resp.Headers["X-Closed"] = "1";
        }

        resp.StatusCode = 200;
        resp.ContentType = "application/octet-stream";
        resp.ContentLength64 = body.Length;

        if (body.Length > 0)
        {
            await resp.OutputStream.WriteAsync(body, 0, body.Length).ConfigureAwait(false);
        }

        resp.Close();

        CleanupIfDone(connId, state);
    }

    private void CleanupIfDone(string connId, ConnState state)
    {
        if (state.ClosedFromClient && state.ClosedFromRemote && state.DownQueue.IsEmpty)
        {
            if (_conns.TryRemove(connId, out _))
            {
                Console.WriteLine($"[TEP:{connId}] Cleaning up connection");
                try { state.Cts.Cancel(); } catch { }
                try { state.Stream.Close(); } catch { }
                try { state.TcpClient.Close(); } catch { }
            }
        }
    }

    public static async Task Main()
    {
        var server = new TepServer();
        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        await server.RunAsync(cts.Token);
        Console.WriteLine("[TEP] Stopped");
    }
}
