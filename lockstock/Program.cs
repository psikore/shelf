using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Threading;


namespace LockStock
{
    [DataContract]
    class EventLog
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Message { get; set; }

        public EventLog(int id, string message)
        {
            Id = id;
            Message = message;
        }
    }

    [DataContract]
    class EventLogger
    {
        [DataMember]
        private List<EventLog> _events = new List<EventLog>();

        private readonly object _lock  = new object();

        public void AddEvent(EventLog eventLog)
        {
            lock (_lock)
            {
                _events.Add(eventLog);
            }
        }

        public static string SerializeToString(EventLogger eventLogger)
        {
            lock (eventLogger._lock)
            {
                var serializer = new DataContractJsonSerializer(typeof(EventLogger));
                using (var ms = new MemoryStream())
                {
                    serializer.WriteObject(ms, eventLogger);
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
        }
    }

    class SnapshotEventLogger
    {
        [IgnoreDataMember]
        private readonly List<EventLog> _events = new List<EventLog>();

        private readonly object _lock = new object();

        public void AddEvent(EventLog eventLog)
        {
            lock (_lock)
            {
                _events.Add(eventLog);
            }
        }

        public string SerializeAndPurge()
        {
            List<EventLog> snapshot;

            // Step 1: Clone the current list
            lock (_lock)
            {
                snapshot = _events.ToList();    // shallow copy
            }

            // Step 2: Serialize the snapshot
            var serializer = new DataContractJsonSerializer (typeof(List<EventLog>));
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, snapshot);
                string json = Encoding.UTF8.GetString (ms.ToArray());

                // Step 3: Remove serialized events 
                lock (_lock)
                {
                    foreach (var e in snapshot)
                    {
                        _events.Remove(e);  // assumes reference equality
                    }
                }
                return json;
            }
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            //var logger = new EventLogger();
            var logger = new SnapshotEventLogger();

            // Task 1: Continously add events
            var writerTask = Task.Run(() =>
            {
                int id = 0;
                while (true)
                {
                    logger.AddEvent(new EventLog(id++, $"Message {id}"));
                    Thread.Sleep(10);   // simulate work
                }
            });

            // Task 2: Continuously serialize
            var readerTask = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        // string json = EventLogger.SerializeToString(logger);
                        string json = logger.SerializeAndPurge();
                        Console.WriteLine(json);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"SerializationBinder failed:\n{ex.Message}");
                    }

                    Thread.Sleep(50);   // simulate interval
                }
            });

            // Wait forever
            Task.WaitAll(writerTask, readerTask);
        }
    }
}
