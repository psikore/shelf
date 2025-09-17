using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.Serialization.Json;
using System.Text;

namespace TwoSmokers
{
    internal class Program
    {
        public delegate void LogHandler(string message);
        
        public delegate string PurgeAndSerializeHandler();

        public static class Logger
        {
            public static LogHandler LogDelegate;

            public static PurgeAndSerializeHandler PurgeAndSerializeDelegate;

            public static void Log(string message)
            {
                LogDelegate?.Invoke(message);
            }

            public static string SerializeAndPurge()
            {
                return PurgeAndSerializeDelegate?.Invoke() ?? string.Empty;
            }
        }

        public class ConsoleLogger
        {
            public void HandleLog(string message)
            {
                Console.WriteLine($"[Console] {message}");
            }
        }

        public class FileLogger
        {
            private readonly string _filePath;

            public FileLogger(string filePath)
            {
                _filePath = filePath;
            }

            public void HandleLog(string message)
            {
                File.AppendAllText(_filePath, $"[File] {message}{Environment.NewLine}");
            }
        }

        public class MemoryLogger
        {
            private readonly ConcurrentQueue<string> _logs = new ConcurrentQueue<string>();

            public void HandleLog(string message)
            {
                _logs.Enqueue($"[Memory] {message}");
            }

            public IReadOnlyCollection<string> PeekLogs()
            {
                // ToArray() gives a safe peek without modifying the queue
                return _logs.ToArray();
            }

            public string PurgeAndSerialize()
            {
                var snapshot = new List<string>();

                // TryDequeue safely drains the queue without locking.
                while (_logs.TryDequeue(out var log))
                {
                    snapshot.Add(log);
                }

                var serializer = new DataContractJsonSerializer(typeof(List<string>));
                using (var ms = new MemoryStream())
                {
                    serializer.WriteObject(ms, snapshot);
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
        }

        static void Main(string[] args)
        {
            var consoleLogger = new ConsoleLogger();
            var fileLogger = new FileLogger("logs.txt");
            var memoryLogger = new MemoryLogger();

            // subscribe each logger's handler to the delegate
            Logger.LogDelegate += consoleLogger.HandleLog;
            Logger.LogDelegate += fileLogger.HandleLog;
            Logger.LogDelegate += memoryLogger.HandleLog;
            Logger.PurgeAndSerializeDelegate += memoryLogger.PurgeAndSerialize;

            // Use the logger
            Logger.Log("System initialized!");
            Logger.Log("Heartbeat OK.");

            string json = Logger.SerializeAndPurge();
            Console.WriteLine(json);
        }
    }
}
