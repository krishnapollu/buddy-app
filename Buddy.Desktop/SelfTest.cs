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
        const string ics = "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nBEGIN:VEVENT\r\nUID:test-meeting\r\nDTSTART:20261025T120000Z\r\nDTEND:20261025T123000Z\r\nRRULE:FREQ=DAILY;COUNT=2\r\nSUMMARY:Team sync\r\nEND:VEVENT\r\nEND:VCALENDAR\r\n";
        var events = CalendarFeed.Parse(ics, now);
        if (events.Count != 2 || events[0].Id == events[1].Id) throw new Exception("ICS recurrence failed.");
        Console.WriteLine("Buddy self-tests passed.");
    }
}
