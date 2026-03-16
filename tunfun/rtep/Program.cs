using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static ConcurrentDictionary<string, TcpClient> Sessions = new();

    static async Task Main(string[] args)
    {
        string prefix = "http://+:8080/";
        var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();

        Console.WriteLine($"[TEP] Listening on {prefix}");

        while (true)
        {
            var ctx = await listener.GetContextAsync();
            _ = Task.Run(() => Handle(ctx));
        }
    }

    static async Task Handle(HttpListenerContext ctx)
    {
        try
        {
            string path = ctx.Request.Url.AbsolutePath;
            string method = ctx.Request.HttpMethod;

            if (path == "/open" && method == "POST")
            {
                await HandleOpen(ctx);
                return;
            }

            if (path.StartsWith("/up/") && method == "POST")
            {
                await HandleUp(ctx);
                return;
            }

            if (path.StartsWith("/down/") && method == "GET")
            {
                await HandleDown(ctx);
                return;
            }

            ctx.Response.StatusCode = 404;
            ctx.Response.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERR] {ex}");
            try
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.Close();
            }
            catch
            {
                
            }
        }
    }

    static async Task HandleOpen(HttpListenerContext ctx)
    {
        using var reader = new StreamReader(ctx.Request.InputStream);
        string body = await reader.ReadToEndAsync();

        var doc = JsonDocument.Parse(body);
        string host = doc.RootElement.GetProperty("dest_host").GetString();
        int port = doc.RootElement.GetProperty("dest_port").GetInt32();

        var client = new TcpClient();
        await client.ConnectAsync(host, port);

        string id = Guid.NewGuid().ToString("N");
        Sessions[id] = client;

        Console.WriteLine($"[OPEN] id={id} -> {host}:{port}");

        var respObj = new { id };
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(respObj);

        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = 200;
        await ctx.Response.OutputStream.WriteAsync(json);
        ctx.Response.Close();
    }

    static async Task HandleUp(HttpListenerContext ctx)
    {
        string id = ctx.Request.Url.AbsolutePath.Substring("/up/".Length);
        bool fin = ctx.Request.QueryString["fin"] == "1";
        if (!Sessions.TryGetValue(id, out var client))
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.Close();
            return;
        }

        NetworkStream ns = client.GetStream();

        await ctx.Request.InputStream.CopyToAsync(ns);
        await ns.FlushAsync();

        if (fin)
        {
            Console.WriteLine($"[UP] FIN received for {id}");
            client.Close();
            Sessions.TryRemove(id, out _);
        }

        ctx.Response.StatusCode = 200;
        ctx.Response.Close();
    }

    static async Task HandleDown(HttpListenerContext ctx)
    {
       string id = ctx.Request.Url.AbsolutePath.Substring("/down/".Length);

        if (!Sessions.TryGetValue(id, out var client))
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.Close();
            return;
        }

        var ns = client.GetStream();

        // Read up to 4096 bytes (non-blocking with timeout)
        byte[] buffer = new byte[4096];

        // If no data available, block briefly but not forever
        if (!ns.DataAvailable)
        {
            await Task.Delay(5);
        }

        int read = 0;
        if (ns.DataAvailable)
        {
            read = await ns.ReadAsync(buffer, 0, buffer.Length);
        }

        if (read == 0)
        {
            // remote closed?
            if (!client.Connected || !ns.CanRead)
            {
                Console.WriteLine($"[DOWN] EOF for {id}");
                Sessions.TryRemove(id, out _);
                ctx.Response.StatusCode = 204;
                ctx.Response.Close();
                return;
            }

            // no data yet -> return empty body (client will poll again)
            ctx.Response.StatusCode = 200;
            ctx.Response.Close();
            return;
        }

        // Return the bytes we read
        ctx.Response.StatusCode = 200;
        await ctx.Response.OutputStream.WriteAsync(buffer, 0, read);
        ctx.Response.Close();
    }
}
