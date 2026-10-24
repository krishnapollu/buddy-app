using System.Text.Json;

namespace Buddy.Desktop;

public sealed class BuddySettings
{
    public string Avatar { get; set; } = "Cat";
    public string? ImagePath { get; set; }
    public bool Visible { get; set; } = true;
    public bool Speak { get; set; } = true;
    public bool CodexEnabled { get; set; }
    public bool ClaudeEnabled { get; set; }
    public bool MoveForReminders { get; set; } = true;
    public int WaterMinutes { get; set; } = 60;
    public int BlinkMinutes { get; set; } = 20;
    public int StretchMinutes { get; set; } = 60;
    public int MeetingLeadMinutes { get; set; } = 5;
    public string IcsUrl { get; set; } = "";
    public double? X { get; set; }
    public double? Y { get; set; }
    public List<Meeting> Meetings { get; set; } = [];
    public Dictionary<string, DateTimeOffset> LastAlerts { get; set; } = [];
}

public sealed class Meeting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public DateTimeOffset StartsAt { get; set; }
}

public static class SettingsStore
{
    public static readonly string DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Buddy");
    public static readonly string FilePath = Path.Combine(DirectoryPath, "settings.json");
    public static BuddySettings Load()
    {
        try { return JsonSerializer.Deserialize<BuddySettings>(File.ReadAllText(FilePath)) ?? new(); }
        catch (IOException) { return new(); }
        catch (JsonException) { return new(); }
    }
    public static void Save(BuddySettings settings)
    {
        Directory.CreateDirectory(DirectoryPath);
        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, FilePath, true);
    }
}

public sealed record Alert(string Key, string Text);

public static class ReminderEngine
{
    public static List<Alert> Due(BuddySettings settings, IEnumerable<Meeting> calendarMeetings, DateTimeOffset now)
    {
        var due = new List<Alert>();
        foreach (var (name, minutes, text) in new[] {
            ("water", settings.WaterMinutes, "Time to drink water"),
            ("blink", settings.BlinkMinutes, "Rest your eyes and blink"),
            ("stretch", settings.StretchMinutes, "Time to take a stretch") })
        {
            if (minutes <= 0) continue;
            var key = "habit:" + name;
            if (!settings.LastAlerts.TryGetValue(key, out var last))
            {
                settings.LastAlerts[key] = now;
                continue;
            }
            if (now - last >= TimeSpan.FromMinutes(minutes)) due.Add(new(key, text));
        }
        foreach (var meeting in settings.Meetings.Concat(calendarMeetings))
        {
            if (meeting.StartsAt < now || meeting.StartsAt > now.AddMinutes(Math.Max(0, settings.MeetingLeadMinutes))) continue;
            var key = "meeting:" + meeting.Id + ":" + meeting.StartsAt.ToUnixTimeSeconds();
            if (!settings.LastAlerts.ContainsKey(key)) due.Add(new(key, "Meeting soon: " + meeting.Title));
        }
        return due;
    }
}
