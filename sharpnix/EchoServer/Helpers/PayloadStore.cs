using Microsoft.Data.Sqlite;

namespace EchoServer.Helpers;

public class PayloadStore
{
    private readonly string _dbPath;

    public PayloadStore(string dbPath)
    {
        _dbPath = dbPath;
    }

    public string? GetPayload(string gadgetName)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath};Mode=ReadOnly");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT payload_base64 FROM payloads WHERE gadget = @gadget LIMIT 1";
        cmd.Parameters.AddWithValue("@gadget", gadgetName);

        return cmd.ExecuteScalar() as string;
    }

    public List<GadgetRecord> ListGadgets()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath};Mode=ReadOnly");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT gadget, description, needs_command FROM payloads ORDER BY gadget";

        var results = new List<GadgetRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new GadgetRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(2) == 1));
        }
        return results;
    }

    public bool DatabaseExists() => File.Exists(_dbPath);
}

public record GadgetRecord(string Name, string Description, bool NeedsCommand);
