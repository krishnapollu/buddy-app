using System.Text.Json;

namespace Buddy.Desktop;

public sealed record HookEvent(string Source, string Message, DateTimeOffset CreatedAt);

public static class AgentHooks
{
    private static readonly string DirectoryPath = Path.Combine(SettingsStore.DirectoryPath, "Events");
    public static void Receive(string[] args)
    {
        var source = args[1];
        if (source is not ("codex" or "claude")) return;
        var settings = SettingsStore.Load();
        if (source == "codex" && !settings.CodexEnabled || source == "claude" && !settings.ClaudeEnabled) return;
        var payload = source == "codex" ? args.ElementAtOrDefault(2) : ReadLimitedStdin();
        if (payload is null || payload.Length > 65_536) return;
        try
        {
            var message = Classify(source, payload);
            if (message is null) return;
            Directory.CreateDirectory(DirectoryPath);
            var path = Path.Combine(DirectoryPath, source + "-" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path, JsonSerializer.Serialize(new HookEvent(source, message, DateTimeOffset.Now)));
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException) { }
    }
    private static string ReadLimitedStdin()
    {
        var buffer = new char[65_537];
        var length = Console.In.ReadBlock(buffer);
        return new string(buffer, 0, length);
    }
    internal static string? Classify(string source, string payload)
    {
        if (payload.Length > 65_536) return null;
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) return null;
        return source == "codex"
            ? root.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String && type.GetString() == "agent-turn-complete" ? "Codex finished" : null
            : root.TryGetProperty("hook_event_name", out var hook) && hook.ValueKind == JsonValueKind.String ? hook.GetString() switch {
                "Stop" => "Claude Code finished", "Notification" => "Claude Code needs attention",
                "StopFailure" => "Claude Code stopped", _ => null } : null;
    }
    public static IEnumerable<string> Drain(BuddySettings settings)
    {
        if (!settings.CodexEnabled && !settings.ClaudeEnabled || !Directory.Exists(DirectoryPath)) yield break;
        foreach (var path in Directory.EnumerateFiles(DirectoryPath, "*.json").Take(20))
        {
            HookEvent? item = null;
            try
            {
                if (new FileInfo(path).Length <= 4_096)
                    item = JsonSerializer.Deserialize<HookEvent>(File.ReadAllText(path));
                File.Delete(path);
            }
            catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException) { }
            if (item is not null && item.Message is ("Codex finished" or "Claude Code finished" or "Claude Code needs attention" or "Claude Code stopped") && Math.Abs((DateTimeOffset.Now - item.CreatedAt).TotalSeconds) < 45 &&
                (item.Source == "codex" && settings.CodexEnabled || item.Source == "claude" && settings.ClaudeEnabled))
                yield return item.Message;
        }
    }
}
