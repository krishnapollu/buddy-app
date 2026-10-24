using Avalonia;

namespace Buddy.Desktop;

internal static class SelfTest
{
    public static void Run()
    {
        var now = new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);
        var settings = new BuddySettings { WaterMinutes = 60, BlinkMinutes = 0, StretchMinutes = 0, Speak = false };
        if (ReminderEngine.Due(settings, [], now).Count != 0) throw new Exception("First run should start habit timer.");
        if (ReminderEngine.Due(settings, [], now.AddMinutes(59)).Count != 0) throw new Exception("Habit fired early.");
        if (ReminderEngine.Due(settings, [], now.AddMinutes(60)).Single().Key != "habit:water") throw new Exception("Habit did not fire.");
        settings.Meetings.Add(new Meeting { Title = "Review", StartsAt = now.AddMinutes(65) });
        if (ReminderEngine.Due(settings, [], now.AddMinutes(60)).Count != 2) throw new Exception("Meeting did not fire.");
        var task = new TaskReminder { Title = "Submit report", DueAt = now.AddMinutes(10) };
        settings.Tasks.Add(task);
        if (ReminderEngine.Due(settings, [], now.AddMinutes(9)).Any(x => x.Key.StartsWith("task:"))) throw new Exception("Task fired early.");
        var taskAlert = ReminderEngine.Due(settings, [], now.AddMinutes(10)).Single(x => x.Key == "task:" + task.Id);
        settings.LastAlerts[taskAlert.Key] = now.AddMinutes(10);
        if (ReminderEngine.Due(settings, [], now.AddMinutes(11)).Any(x => x.Key == taskAlert.Key)) throw new Exception("Task repeated.");
        task.IsDone = true;
        settings.LastAlerts.Remove(taskAlert.Key);
        if (ReminderEngine.Due(settings, [], now.AddMinutes(11)).Any(x => x.Key == taskAlert.Key)) throw new Exception("Completed task fired.");
        if (Avatars.All.Length < 16 || Avatars.All.Select(x => x.Name).Distinct().Count() != Avatars.All.Length) throw new Exception("Avatar catalog invalid.");
        var area = new PixelRect(100, 50, 1000, 700);
        if (MainWindow.ReminderTarget(area, new PixelPoint(900, 500), "Center") != new PixelPoint(505, 305))
            throw new Exception("Center reminder target invalid.");
        if (MainWindow.ReminderTarget(area, new PixelPoint(900, 500), "Cross") != new PixelPoint(120, 305) ||
            MainWindow.ReminderTarget(area, new PixelPoint(200, 500), "Cross") != new PixelPoint(890, 305))
            throw new Exception("Cross-screen reminder target invalid.");
        var takeoff = MainWindow.HopFrame(0, 14, 1);
        var airborne = MainWindow.HopFrame(7, 14, 1);
        var landing = MainWindow.HopFrame(14, 14, 1);
        if (takeoff.Fraction != 0 || takeoff.Lift != 0 || airborne.Fraction != 0.5 || airborne.Lift < 0.99 ||
            landing.Fraction != 1 || landing.Lift != 0) throw new Exception("Reminder hop cycle invalid.");
        const string ics = "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nBEGIN:VEVENT\r\nUID:test-meeting\r\nDTSTART:20261025T120000Z\r\nDTEND:20261025T123000Z\r\nRRULE:FREQ=DAILY;COUNT=2\r\nSUMMARY:Team sync\r\nEND:VEVENT\r\nEND:VCALENDAR\r\n";
        var events = CalendarFeed.Parse(ics, now);
        if (events.Count != 2 || events[0].Id == events[1].Id) throw new Exception("ICS recurrence failed.");
        if (AgentHooks.Classify("codex", "{\"type\":\"agent-turn-complete\",\"prompt\":\"private\"}") != "Codex finished") throw new Exception("Codex hook failed.");
        if (AgentHooks.Classify("claude", "{\"hook_event_name\":\"Notification\",\"message\":\"private\"}") != "Claude Code needs attention") throw new Exception("Claude hook failed.");
        Console.WriteLine("Buddy self-tests passed.");
    }
}
