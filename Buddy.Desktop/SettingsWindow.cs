using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace Buddy.Desktop;

public sealed class SettingsWindow : Window
{
    public bool AllowClose { get; set; }
    private readonly BuddySettings settings;
    private readonly Action changed;
    private readonly TextBlock feedStatus = new() { Text = "Calendar not synced yet", TextWrapping = TextWrapping.Wrap };
    private readonly StackPanel meetings = new() { Spacing = 5 };
    public SettingsWindow(BuddySettings settings, Action changed, Action refreshFeed, Action show, Action reset, Action quit)
    {
        this.settings = settings; this.changed = changed;
        Title = "Buddy settings"; Width = 520; Height = 660; MinWidth = 430; MinHeight = 500;
        var body = new StackPanel { Margin = new Thickness(18), Spacing = 12 };
        body.Children.Add(new TextBlock { Text = "Buddy", FontSize = 25, FontWeight = FontWeight.Bold });
        body.Children.Add(new TextBlock { Text = "Choose a companion" });
        var avatar = new ComboBox { ItemsSource = Avatars.All.Select(x => $"{x.Category} · {x.Emoji} {x.Name}").ToList(), SelectedIndex = Math.Max(0, Array.FindIndex(Avatars.All, x => x.Name == settings.Avatar)) };
        avatar.SelectionChanged += (_, _) => { if (avatar.SelectedIndex < 0) return; settings.Avatar = Avatars.All[avatar.SelectedIndex].Name; settings.ImagePath = null; Save(); };
        body.Children.Add(avatar);
        var import = Button("Import image…", async () => {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Images") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp"] }] });
            if (files.Count == 0) return;
            var file = files[0];
            var extension = Path.GetExtension(file.Name).ToLowerInvariant();
            if (extension is not (".png" or ".jpg" or ".jpeg" or ".webp")) return;
            Directory.CreateDirectory(SettingsStore.DirectoryPath);
            var destination = Path.Combine(SettingsStore.DirectoryPath, "avatar" + extension);
            await using var source = await file.OpenReadAsync();
            await using var target = File.Create(destination);
            await source.CopyToAsync(target);
            settings.ImagePath = destination; Save();
        });
        body.Children.Add(import);
        body.Children.Add(new TextBlock { Text = "Habit reminders (minutes; 0 turns one off)", FontWeight = FontWeight.SemiBold });
        body.Children.Add(Interval("Drink water", settings.WaterMinutes, x => settings.WaterMinutes = x));
        body.Children.Add(Interval("Blink eyes", settings.BlinkMinutes, x => settings.BlinkMinutes = x));
        body.Children.Add(Interval("Take a stretch", settings.StretchMinutes, x => settings.StretchMinutes = x));
        body.Children.Add(Interval("Meeting lead time", settings.MeetingLeadMinutes, x => settings.MeetingLeadMinutes = x));
        var speak = new CheckBox { Content = "Speak reminders", IsChecked = settings.Speak };
        speak.IsCheckedChanged += (_, _) => { settings.Speak = speak.IsChecked == true; Save(); };
        body.Children.Add(speak);
        var move = new CheckBox { Content = "Move Buddy when reminding", IsChecked = settings.MoveForReminders };
        move.IsCheckedChanged += (_, _) => { settings.MoveForReminders = move.IsChecked == true; Save(); };
        body.Children.Add(move);
        body.Children.Add(new TextBlock { Text = "Agent status hooks", FontWeight = FontWeight.SemiBold });
        var codex = new CheckBox { Content = "Codex CLI", IsChecked = settings.CodexEnabled };
        codex.IsCheckedChanged += (_, _) => { settings.CodexEnabled = codex.IsChecked == true; Save(); };
        body.Children.Add(codex);
        var claude = new CheckBox { Content = "Claude Code", IsChecked = settings.ClaudeEnabled };
        claude.IsCheckedChanged += (_, _) => { settings.ClaudeEnabled = claude.IsChecked == true; Save(); };
        body.Children.Add(claude);
        body.Children.Add(new TextBlock { Text = "ICS calendar feed (HTTPS)", FontWeight = FontWeight.SemiBold });
        var feed = new TextBox { Text = settings.IcsUrl, PlaceholderText = "https://example.com/calendar.ics" };
        feed.LostFocus += (_, _) => { settings.IcsUrl = feed.Text?.Trim() ?? ""; Save(); refreshFeed(); };
        body.Children.Add(feed);
        body.Children.Add(feedStatus);
        body.Children.Add(new TextBlock { Text = "Manual meeting", FontWeight = FontWeight.SemiBold });
        var title = new TextBox { PlaceholderText = "Meeting name" };
        var time = new TextBox { PlaceholderText = "YYYY-MM-DD HH:MM (local time)" };
        body.Children.Add(title); body.Children.Add(time);
        var validation = new TextBlock { Foreground = Brushes.IndianRed };
        body.Children.Add(validation);
        body.Children.Add(Button("Add meeting", () => {
            if (string.IsNullOrWhiteSpace(title.Text) || !DateTime.TryParseExact(time.Text, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var local) || local <= DateTime.Now)
            { validation.Text = "Enter a name and a future local time as YYYY-MM-DD HH:MM."; return; }
            validation.Text = "";
            settings.Meetings.Add(new Meeting { Title = title.Text.Trim(), StartsAt = new DateTimeOffset(local) });
            title.Text = ""; time.Text = ""; Save(); RefreshMeetings();
        }));
        body.Children.Add(meetings);
        RefreshMeetings();
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actions.Children.Add(Button("Show Buddy", show));
        actions.Children.Add(Button("Reset position", reset));
        actions.Children.Add(Button("Quit", quit));
        body.Children.Add(actions);
        Content = new ScrollViewer { Content = body };
        Closing += (_, e) => { if (!AllowClose) { e.Cancel = true; Hide(); } };
    }
    public void SetFeedStatus(string text) => feedStatus.Text = text;
    private Control Interval(string label, int current, Action<int> set)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(new TextBlock { Text = label, Width = 150, VerticalAlignment = VerticalAlignment.Center });
        var input = new NumericUpDown { Minimum = 0, Maximum = 1440, Increment = 1, Value = current, Width = 130 };
        input.ValueChanged += (_, _) => { set((int)(input.Value ?? 0)); Save(); };
        row.Children.Add(input);
        return row;
    }
    private void RefreshMeetings()
    {
        meetings.Children.Clear();
        foreach (var item in settings.Meetings.Where(x => x.StartsAt > DateTimeOffset.Now).OrderBy(x => x.StartsAt).ToList())
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            row.Children.Add(new TextBlock { Text = $"{item.StartsAt.LocalDateTime:MMM d h:mm tt}  {item.Title}", Width = 350, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis });
            row.Children.Add(Button("Remove", () => { settings.Meetings.RemoveAll(x => x.Id == item.Id); Save(); RefreshMeetings(); }));
            meetings.Children.Add(row);
        }
    }
    private static Button Button(string label, Action click)
    {
        var button = new Button { Content = label };
        button.Click += (_, _) => click();
        return button;
    }
    private void Save() { SettingsStore.Save(settings); changed(); }
}
