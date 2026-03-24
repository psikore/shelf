using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SharpTep
{
    // --- TunnelConnection class ---
    public class TunnelConnection
    {
        public string Id { get; }
        public TcpClient TcpClient { get; }
        public NetworkStream Stream { get; }
        public BlockingCollection<byte[]> DownQueue { get; } = new();
        public CancellationTokenSource Cts { get; } = new();

        public TunnelConnection(string id, string targetHost, int targetPort)
        {
            Id = id;
            TcpClient = new TcpClient();
            TcpClient.Connect(targetHost, targetPort);
            Stream = TcpClient.GetStream();
            _ = StartReaderAsync();
        }

        public async Task StartReaderAsync()
        {
            var buffer = new byte[16 * 1024];
            try
            {
                while (!Cts.IsCancellationRequested)
                {
                    int read = await Stream.ReadAsync(buffer, 0, buffer.Length, Cts.Token);
                    if (read <= 0)
                        break;

                    var chunk = new byte[read];
                    Buffer.BlockCopy(buffer, 0, chunk, 0, read);
                    DownQueue.Add(chunk);
                }
            }
            catch { }
            finally
            {
                DownQueue.CompleteAdding();
            }
        }

        public async Task WriteAsync(byte[] data)
        {
            await Stream.WriteAsync(data, 0, data.Length);
            await Stream.FlushAsync();
        }

        public void Close()
        {
            try { Cts.Cancel(); } catch { }
            try { Stream.Close(); } catch { }
            try { TcpClient.Close(); } catch { }
            DownQueue.CompleteAdding();
        }
    }

    // --- TunnelRegistry class ---
    public class TunnelRegistry
    {
        private readonly ConcurrentDictionary<string, TunnelConnection> _connections = new();
        private readonly string _targetHost;
        private readonly int _targetPort;

        public TunnelRegistry(string targetHost, int targetPort)
        {
            _targetHost = targetHost;
            _targetPort = targetPort;
        }

        public TunnelConnection GetOrAdd(string id)
        {
            return _connections.GetOrAdd(id, key =>
            {
                return new TunnelConnection(key, _targetHost, _targetPort);
            });
        }

        public void Close(string id)
        {
            if (_connections.TryRemove(id, out var conn))
                conn.Close();
        }
    }

    // --- Server class ---
    public class Server
    {
        public static async Task Serve()
        {
            var registry = new TunnelRegistry("127.0.0.1", 5000);
            var listener = new HttpListener();
            listener.Prefixes.Add("http://+:8080/tunfun/");
            listener.Start();
            Console.WriteLine("[server] listening on /tunfun");

            while (true)
            {
                var ctx = await listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(ctx, registry));
            }
        }

        public static async Task HandleRequest(HttpListenerContext ctx, TunnelRegistry registry)
        {
            try
            {
                string conn_id = ctx.Request.Headers["X-Conn-ID"];
                if (string.IsNullOrEmpty(conn_id))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.Close();
                    return;
                }

                var conn = registry.GetOrAdd(conn_id);

                if (ctx.Request.HttpMethod == "POST")
                {
                    bool close = ctx.Request.Headers["X-Close"] == "1";
                    if (close)
                    {
                        registry.Close(conn_id);
                        ctx.Response.StatusCode = 200;
                        ctx.Response.Close();
                        return;
                    }

                    using var ms = new MemoryStream();
                    await ctx.Request.InputStream.CopyToAsync(ms);
                    var data = ms.ToArray();

                    if (data.Length > 0)
                        await conn.WriteAsync(data);

                    ctx.Response.StatusCode = 200;
                    ctx.Response.Close();
                    return;
                }

                if (ctx.Request.HttpMethod == "GET")
                {
                    byte[] chunk;

                    try
                    {
                        if (!conn.DownQueue.TryTake(out chunk, TimeSpan.FromSeconds(25)))
                        {
                            ctx.Response.StatusCode = 204;
                            ctx.Response.Close();
                            return;
                        }
                    }
                    catch
                    {
                        ctx.Response.AddHeader("X-Closed", "1");
                        ctx.Response.StatusCode = 200;
                        ctx.Response.Close();
                        return;
                    }

                    await ctx.Response.OutputStream.WriteAsync(chunk, 0, chunk.Length);

                    if (conn.DownQueue.IsCompleted)
                        ctx.Response.AddHeader("X-Closed", "1");

                    ctx.Response.StatusCode = 200;
                    ctx.Response.Close();
                    return;
                }

                ctx.Response.StatusCode = 405;
                ctx.Response.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[server] error {e}");
                try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { }
            }
        }
    }
}
