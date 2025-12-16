using System;
using System.Collections.Concurrent;
using System.Data.SqlTypes;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    // Track progress per client (by IP but could be a session token)
    static ConcurrentDictionary<string, long> clientProgress = new ConcurrentDictionary<string, long>();

    static void Main(string[] args)
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();
        Console.WriteLine("Listening on localhost 8080");

        // Run listener loop in a long-running task
        Task.Factory.StartNew(() =>
        {
            while (true)
            {
                HttpListenerContext ctx = listener.GetContext();
                Task.Run(() => HandleRequest(ctx));
            }
        }, TaskCreationOptions.LongRunning);

        Console.WriteLine("Press Enter to quit");
        Console.ReadLine();
    }

    static void HandleRequest(HttpListenerContext ctx)
    {
        string filePath = @"C:\temp\largefile.dat";
        long fileLength = new FileInfo(filePath).Length;
        const int chunkSize = 128 * 1024;   // 128KB chunks

        HttpListenerRequest request = ctx.Request;
        HttpListenerResponse response = ctx.Response;

        response.ContentType = "application/octet-stream";
        response.AddHeader("Accept-Ranges", "bytes");

        string clientId = ctx.Request.RemoteEndPoint.ToString();

        // Default start position: resume from last known offset
        long start = clientProgress.GetOrAdd(clientId, 0);
        long end = fileLength - 1;

        // If the client explicitly sends Range, override
        string rangeHeader = request.Headers["Range"];
        if (!string.IsNullOrEmpty(rangeHeader))
        {
            // Example: "bytes=1000-2000"
            string[] parts = rangeHeader.Replace("bytes=", "").Split('-');
            if (long.TryParse(parts[0], out long rangeStart))
            {
                start = rangeStart;                
            }

            if (parts.Length > 1 && long.TryParse(parts[1], out long rangeEnd))
            {
                end = rangeEnd;
            }

            if (end >= fileLength)
            {
                end = fileLength - 1;
            }

            response.StatusCode = (int)HttpStatusCode.PartialContent;
            response.AddHeader("Content-Range", $"bytes {start}-{end}/{fileLength}");
        }

        long bytesRemaining = end - start + 1;
        response.ContentLength64 = bytesRemaining;

        try
        {
            using (var mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read))
            {
                long position = start;
                while (position < end)
                {
                    int bytesToRead = (int)Math.Min(chunkSize, end - position + 1);
                    using (var accessor = mmf.CreateViewAccessor(position, bytesToRead, MemoryMappedFileAccess.Read))
                    {
                        byte[] buffer = new byte[bytesToRead];
                        accessor.ReadArray(0, buffer, 0, bytesToRead);

                        response.OutputStream.Write(buffer, 0, bytesToRead);
                        response.OutputStream.Flush();

                        // Track chunk progress persistently
                        clientProgress[clientId] = position + bytesToRead;
                        Console.WriteLine($"[{clientId}] Sent chunk {position}-{position + bytesToRead - 1}");
                    }
                    position += bytesToRead;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Err: " + ex.Message);
        }
        finally
        {
            response.OutputStream.Close();
        }
    }
}