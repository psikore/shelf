using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

[DataContract]
public class EventLog
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

class SnapshotEventLogger
{
    [IgnoreDataMember]
    private readonly ConcurrentQueue<EventLog> _events = new ConcurrentQueue<EventLog>();

    public void AddEvent(EventLog eventLog)
    {
        _events.Enqueue(eventLog);
    }

    public string SerializeAndPurge()
    {
        var snapshot = new List<EventLog>();

        // Step 1: Drain the queue
        while (_events.TryDequeue(out var log))
        {
            snapshot.Add(log);
        }

        // Step 2: Serialize the snapshot
        var serializer = new DataContractJsonSerializer(typeof(List<EventLog>));
        using (var ms = new MemoryStream())
        {
            serializer.WriteObject(ms, snapshot);
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}