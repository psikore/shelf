using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace torrunt
{
    public class ChunkHash
    {
        public int Index { get; set; }
        public string Sha256 { get; set; }
    }

    public class FileManifest
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public int ChunkSize { get; set; }
        public List<ChunkHash> Chunks { get; set; } = new List<ChunkHash>();
    }

    public static class FileHasher
    {
        public static FileManifest ComputeManifest(string path, int chunkSize)
        {
            var manifest = new FileManifest
            {
                FileName = Path.GetFileName(path),
                FileSize = new FileInfo(path).Length,
                ChunkSize = chunkSize
            };

            byte[] buffer = new byte[1024 * 64]; // 64 KiB read buffer
            int chunkIndex = 0;

            using (var stream = File.OpenRead(path))
            {
                long bytesInChunk = 0;
                using (var sha = SHA256.Create())
                {
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        int offset = 0;

                        while (read > 0)
                        {
                            long remainingInChunk = chunkSize - bytesInChunk;
                            int toHash = (int)Math.Min(remainingInChunk, read);

                            sha.TransformBlock(buffer, offset, toHash, null, 0);

                            bytesInChunk += toHash;
                            offset += toHash;
                            read -= toHash;

                            if (bytesInChunk == chunkSize)
                            {
                                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                                manifest.Chunks.Add(new ChunkHash
                                {
                                    Index = chunkIndex++,
                                    Sha256 = BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant()
                                });

                                sha.Initialize();
                                bytesInChunk = 0;
                            }
                        }
                    }

                    // Final partial chunk
                    if (bytesInChunk > 0)
                    {
                        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                        manifest.Chunks.Add(new ChunkHash
                        {
                            Index = chunkIndex,
                            Sha256 = BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant()
                        });
                    }
                }
            }

            return manifest;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            string filename = args[0];
            const  int chunkSize = 1 * 1024 * 1024;
            FileManifest fileManifest = FileHasher.ComputeManifest(filename, chunkSize);
            Console.WriteLine($"Manifest for: {fileManifest.FileName}");
            Console.WriteLine($"FileSize: {fileManifest.FileSize}");
            Console.WriteLine($"ChunkSize: {fileManifest.ChunkSize}");
            foreach (var chunk in fileManifest.Chunks)
            {
                Console.WriteLine($"{chunk.Index}: {chunk.Sha256.ToString()}");
            }
        }
    }
}
