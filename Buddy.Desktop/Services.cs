using System.Diagnostics;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace Buddy.Desktop;

public sealed class CalendarFeed
{
    private readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(12) };
    public async Task<List<Meeting>> FetchAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address)) return [];
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Use an HTTPS ICS feed URL.");
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > 2_000_000) throw new InvalidDataException("ICS feed is too large.");
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (content.Length > 2_000_000) throw new InvalidDataException("ICS feed is too large.");
        return Parse(content);
    }
    internal static List<Meeting> Parse(string content, DateTimeOffset? reference = null)
    {
        var calendar = Calendar.Load(content);
        var now = reference ?? DateTimeOffset.Now;
        if (calendar is null) return [];
        return calendar.GetOccurrences(new CalDateTime(now.UtcDateTime.AddHours(-1)))
            .TakeWhile(o => o.Period.StartTime.AsUtc < now.UtcDateTime.AddDays(30))
            .Where(o => o.Source is CalendarEvent)
            .Select(o => new Meeting {
                Id = StableId(((CalendarEvent)o.Source).Uid ?? "", o.Period.StartTime.AsUtc),
                Title = string.IsNullOrWhiteSpace(((CalendarEvent)o.Source).Summary) ? "Untitled meeting" : ((CalendarEvent)o.Source).Summary!,
                StartsAt = new DateTimeOffset(o.Period.StartTime.AsUtc, TimeSpan.Zero)
            })
            .Where(m => m.StartsAt >= now.AddHours(-1))
            .OrderBy(m => m.StartsAt).Take(500).ToList();
    }
    private static Guid StableId(string uid, DateTime start)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(uid + start.ToString("O")));
        return new Guid(bytes);
    }
}

public static class Speech
{
    public static void Say(string message)
    {
        try
        {
            if (OperatingSystem.IsMacOS())
                Process.Start(new ProcessStartInfo("/usr/bin/say") { ArgumentList = { message }, UseShellExecute = false, CreateNoWindow = true });
            else if (OperatingSystem.IsWindows())
            {
                var script = "Add-Type -AssemblyName System.Speech; $s = New-Object System.Speech.Synthesis.SpeechSynthesizer; $s.Speak([Console]::In.ReadToEnd())";
                var process = Process.Start(new ProcessStartInfo("powershell.exe") {
                    ArgumentList = { "-NoProfile", "-NonInteractive", "-Command", script },
                    RedirectStandardInput = true, UseShellExecute = false, CreateNoWindow = true });
                if (process is not null) { process.StandardInput.Write(message); process.StandardInput.Close(); }
            }
        }
        catch (System.ComponentModel.Win32Exception) { }
    }
}

public static class Avatars
{
    public static readonly (string Category, string Name, string Emoji)[] All = [
        ("Pets", "Cat", "🐱"), ("Pets", "Dog", "🐶"), ("Pets", "Rabbit", "🐰"),
        ("Pets", "Fox", "🦊"), ("Pets", "Panda", "🐼"), ("Pets", "Owl", "🦉"),
        ("Fantasy", "Dragon", "🐲"), ("Fantasy", "Unicorn", "🦄"), ("Fantasy", "Alien", "👽"),
        ("Cartoons", "Robot", "🤖"), ("Cartoons", "Ghost", "👻"), ("Cartoons", "Cloud", "☁️"),
        ("Cinema", "Star", "🌟"), ("Cinema", "Director", "🎬"), ("Cinema", "Detective", "🕵️"),
        ("Cinema", "Hero", "🦸"), ("Nature", "Flower", "🌻"), ("Nature", "Mushroom", "🍄")
    ];
    public static string Emoji(string name) => All.FirstOrDefault(x => x.Name == name).Emoji ?? "🐱";
}
