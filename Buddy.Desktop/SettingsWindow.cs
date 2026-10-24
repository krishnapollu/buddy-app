using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace Buddy.Desktop;

public sealed class SettingsWindow : Window
{
    public bool AllowClose { get; set; }
    private readonly BuddySettings settings;
    private readonly Action avatarChanged;
    private readonly TextBlock feedStatus = new() { Text = "Calendar not synced yet", TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock importStatus = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock voiceStatus = new() { TextWrapping = TextWrapping.Wrap };
    private readonly StackPanel tasks = new() { Spacing = 8 };
    private readonly StackPanel meetings = new() { Spacing = 8 };
    private readonly List<Button> avatarButtons = [];
    private readonly ComboBox voices = new() { MinWidth = 260 };
    private bool loadingVoices;

    public SettingsWindow(BuddySettings settings, Action avatarChanged, Action refreshFeed, Action show, Action reset, Action quit)
    {
        this.settings = settings; this.avatarChanged = avatarChanged;
        Title = "Buddy settings"; Width = 730; Height = 720; MinWidth = 630; MinHeight = 570;
        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Margin = new Thickness(20) };
        var heading = new StackPanel { Spacing = 3, Margin = new Thickness(0, 0, 0, 16) };
        heading.Children.Add(new TextBlock { Text = "Buddy settings", FontSize = 26, FontWeight = FontWeight.Bold });
        heading.Children.Add(new TextBlock { Text = "Make your companion yours and plan the reminders that matter." });
        root.Children.Add(heading);
        var tabs = new TabControl { ItemsSource = new TabItem[] {
            Tab("Companion", CompanionTab()), Tab("Reminders", RemindersTab()),
            Tab("Calendar", CalendarTab(refreshFeed)), Tab("Integrations", IntegrationsTab()) } };
        Grid.SetRow(tabs, 1); root.Children.Add(tabs);
        var footer = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 14, 0, 0) };
        footer.Children.Add(Button("Show Buddy", show));
        footer.Children.Add(Button("Reset position", reset));
        footer.Children.Add(Button("Quit Buddy", quit));
        Grid.SetRow(footer, 2); root.Children.Add(footer);
        Content = root;
        Closing += (_, e) => { if (!AllowClose) { e.Cancel = true; Hide(); } };
        Opened += async (_, _) => await LoadVoices();
    }

    private static TabItem Tab(string title, Control content) => new() { Header = title, Content = new ScrollViewer { Content = content } };
    private static Border Card(Control content) => new() { Child = content, BorderBrush = Brushes.Gray,
        BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(16), Margin = new Thickness(0, 6, 0, 14) };
    private static TextBlock SectionTitle(string text) => new() { Text = text, FontSize = 18, FontWeight = FontWeight.SemiBold };
    private static Button Button(string label, Action click)
    {
        var button = new Button { Content = label };
        button.Click += (_, _) => click();
        return button;
    }
    private static StackPanel Section() => new() { Margin = new Thickness(12, 18, 12, 8), Spacing = 10 };
    private void Save(bool refreshAvatar = false) { SettingsStore.Save(settings); if (refreshAvatar) avatarChanged(); }

    private Control CompanionTab()
    {
        var body = Section();
        body.Children.Add(SectionTitle("Choose an avatar"));
        body.Children.Add(new TextBlock { Text = "Original character art. Select one below or import your own photo.", TextWrapping = TextWrapping.Wrap });
        var import = Button("Import and crop image…", async () => await ImportImage());
        body.Children.Add(import); body.Children.Add(importStatus);
        foreach (var group in Avatars.All.GroupBy(x => x.Category))
        {
            body.Children.Add(SectionTitle(group.Key));
            foreach (var chunk in group.Chunk(4))
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                foreach (var avatar in chunk)
                {
                    var column = new StackPanel { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Center };
                    column.Children.Add(new AvatarView(avatar) { Width = 105, Height = 105 });
                    column.Children.Add(new TextBlock { Text = avatar.Name, HorizontalAlignment = HorizontalAlignment.Center });
                    var card = new Button { Content = column, Width = 140, Height = 140, Tag = avatar.Name };
                    card.Click += (_, _) => { settings.Avatar = avatar.Name; settings.ImagePath = null; Save(true); HighlightAvatar(); importStatus.Text = ""; };
                    avatarButtons.Add(card); row.Children.Add(card);
                }
                body.Children.Add(row);
            }
        }
        HighlightAvatar();
        return body;
    }
    private void HighlightAvatar()
    {
        foreach (var button in avatarButtons)
            button.BorderBrush = settings.ImagePath is null && (string?)button.Tag == settings.Avatar ? Brushes.DodgerBlue : Brushes.Gray;
    }
    private async Task ImportImage()
    {
        importStatus.Text = "";
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false,
                Title = "Choose an avatar image", FileTypeFilter = [new FilePickerFileType("Images") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp"] }] });
            if (files.Count == 0) return;
            await using var source = await files[0].OpenReadAsync();
            using var data = new MemoryStream();
            var buffer = new byte[81_920];
            int count;
            while ((count = await source.ReadAsync(buffer)) > 0)
            {
                if (data.Length + count > 12_000_000) throw new InvalidDataException("Choose an image under 12 MB.");
                data.Write(buffer, 0, count);
            }
            var crop = new CropWindow(data.ToArray());
            var path = await crop.ShowDialog<string?>(this);
            if (path is null) return;
            settings.ImagePath = path; Save(true); HighlightAvatar();
            importStatus.Text = "Your cropped image is now the active avatar.";
        }
        catch (Exception e)
        { importStatus.Text = "Image import failed: " + e.Message; }
    }

    private Control RemindersTab()
    {
        var body = Section();
        var habits = new StackPanel { Spacing = 12 };
        habits.Children.Add(SectionTitle("Healthy habits"));
        habits.Children.Add(new TextBlock { Text = "Interval in minutes. Set 0 to turn a reminder off." });
        habits.Children.Add(Interval("Drink water", settings.WaterMinutes, x => settings.WaterMinutes = x));
        habits.Children.Add(Interval("Blink eyes", settings.BlinkMinutes, x => settings.BlinkMinutes = x));
        habits.Children.Add(Interval("Take a stretch", settings.StretchMinutes, x => settings.StretchMinutes = x));
        body.Children.Add(Card(habits));
        var taskSection = new StackPanel { Spacing = 10 };
        taskSection.Children.Add(SectionTitle("Task reminders"));
        taskSection.Children.Add(new TextBlock { Text = "Add a task with a due date and time. It stays in the list until you complete or remove it." });
        taskSection.Children.Add(ReminderEntry("Task name", "Add task", false));
        taskSection.Children.Add(tasks);
        RefreshTasks(); body.Children.Add(Card(taskSection));
        var options = new StackPanel { Spacing = 8 };
        options.Children.Add(SectionTitle("Reminder behavior"));
        var speak = new CheckBox { Content = "Speak reminders", IsChecked = settings.Speak };
        speak.IsCheckedChanged += (_, _) => { settings.Speak = speak.IsChecked == true; Save(); };
        options.Children.Add(speak);
        var move = new CheckBox { Content = "Move Buddy when reminding", IsChecked = settings.MoveForReminders };
        move.IsCheckedChanged += (_, _) => { settings.MoveForReminders = move.IsChecked == true; Save(); };
        options.Children.Add(move);
        body.Children.Add(Card(options));
        return body;
    }
    private Control CalendarTab(Action refreshFeed)
    {
        var body = Section();
        var manual = new StackPanel { Spacing = 10 };
        manual.Children.Add(SectionTitle("Manual meetings"));
        manual.Children.Add(ReminderEntry("Meeting name", "Add meeting", true));
        manual.Children.Add(meetings); RefreshMeetings();
        body.Children.Add(Card(manual));
        var feedSection = new StackPanel { Spacing = 10 };
        feedSection.Children.Add(SectionTitle("Calendar feed"));
        feedSection.Children.Add(new TextBlock { Text = "Paste a private HTTPS ICS link. Buddy checks it every five minutes.", TextWrapping = TextWrapping.Wrap });
        var feed = new TextBox { Text = settings.IcsUrl, PlaceholderText = "https://example.com/calendar.ics" };
        feed.LostFocus += (_, _) => {
            var value = feed.Text?.Trim() ?? "";
            if (settings.IcsUrl == value) return;
            settings.IcsUrl = value; Save(); refreshFeed();
        };
        feedSection.Children.Add(feed); feedSection.Children.Add(feedStatus);
        feedSection.Children.Add(Interval("Alert before meetings (minutes)", settings.MeetingLeadMinutes, x => settings.MeetingLeadMinutes = x));
        body.Children.Add(Card(feedSection));
        return body;
    }
    private Control IntegrationsTab()
    {
        var body = Section();
        var voice = new StackPanel { Spacing = 10 };
        voice.Children.Add(SectionTitle("Voice"));
        voice.Children.Add(new TextBlock { Text = "Choose an installed system voice, then play a sample.", TextWrapping = TextWrapping.Wrap });
        voices.ItemsSource = new List<VoiceOption> { new("Default", "System default") };
        voices.SelectedIndex = 0;
        voices.SelectionChanged += (_, _) => { if (loadingVoices) return; if (voices.SelectedItem is VoiceOption selected) { settings.Voice = selected.Name; Save(); } };
        voice.Children.Add(voices);
        voice.Children.Add(Button("Test voice", () => { Speech.Say("Hello! I'm Buddy, here to help you stay on track.", settings.Voice); voiceStatus.Text = "Playing sample with " + settings.Voice + "."; }));
        voice.Children.Add(voiceStatus);
        body.Children.Add(Card(voice));
        var agents = new StackPanel { Spacing = 8 };
        agents.Children.Add(SectionTitle("Local agent status"));
        agents.Children.Add(new TextBlock { Text = "These hooks show local completion and attention messages when configured in the CLI.", TextWrapping = TextWrapping.Wrap });
        var codex = new CheckBox { Content = "Codex CLI", IsChecked = settings.CodexEnabled };
        codex.IsCheckedChanged += (_, _) => { settings.CodexEnabled = codex.IsChecked == true; Save(); };
        agents.Children.Add(codex);
        var claude = new CheckBox { Content = "Claude Code", IsChecked = settings.ClaudeEnabled };
        claude.IsCheckedChanged += (_, _) => { settings.ClaudeEnabled = claude.IsChecked == true; Save(); };
        agents.Children.Add(claude);
        body.Children.Add(Card(agents));
        return body;
    }
    private async Task LoadVoices()
    {
        var options = await Task.Run(Speech.AvailableVoices);
        var selectedName = settings.Voice;
        loadingVoices = true;
        voices.ItemsSource = options;
        voices.SelectedItem = options.FirstOrDefault(x => x.Name == selectedName) ?? options[0];
        loadingVoices = false;
        voiceStatus.Text = options.Count > 1 ? $"{options.Count - 1} installed voices available." : "No extra system voices found.";
    }
    private Control Interval(string label, int current, Action<int> set)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        row.Children.Add(new TextBlock { Text = label, Width = 265, VerticalAlignment = VerticalAlignment.Center });
        var input = new NumericUpDown { Minimum = 0, Maximum = 1440, Increment = 1, Value = current, Width = 130 };
        input.ValueChanged += (_, _) => { set((int)(input.Value ?? 0)); Save(); };
        row.Children.Add(input); return row;
    }
    private Control ReminderEntry(string placeholder, string action, bool meeting)
    {
        var panel = new StackPanel { Spacing = 8 };
        var name = new TextBox { PlaceholderText = placeholder };
        var date = new DatePicker { SelectedDate = DateTimeOffset.Now };
        var time = new TimePicker { SelectedTime = DateTime.Now.TimeOfDay.Add(TimeSpan.FromHours(1)) };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(date); row.Children.Add(time);
        var error = new TextBlock { Foreground = Brushes.IndianRed, TextWrapping = TextWrapping.Wrap };
        panel.Children.Add(name); panel.Children.Add(row); panel.Children.Add(error);
        panel.Children.Add(Button(action, () => {
            if (string.IsNullOrWhiteSpace(name.Text) || date.SelectedDate is null || time.SelectedTime is null)
            { error.Text = "Enter a name, date, and time."; return; }
            var local = date.SelectedDate.Value.Date + time.SelectedTime.Value;
            if (local <= DateTime.Now) { error.Text = "Choose a future date and time."; return; }
            var startsAt = new DateTimeOffset(local);
            if (meeting) { settings.Meetings.Add(new Meeting { Title = name.Text.Trim(), StartsAt = startsAt }); RefreshMeetings(); }
            else { settings.Tasks.Add(new TaskReminder { Title = name.Text.Trim(), DueAt = startsAt }); RefreshTasks(); }
            name.Text = ""; error.Text = ""; Save();
        }));
        return panel;
    }
    private void RefreshTasks()
    {
        tasks.Children.Clear();
        foreach (var task in settings.Tasks.OrderBy(x => x.IsDone).ThenBy(x => x.DueAt).ToList())
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var done = new CheckBox { IsChecked = task.IsDone, VerticalAlignment = VerticalAlignment.Center };
            done.IsCheckedChanged += (_, _) => { task.IsDone = done.IsChecked == true; Save(); RefreshTasks(); };
            row.Children.Add(done);
            row.Children.Add(new TextBlock { Text = $"{task.Title}  ·  {task.DueAt.LocalDateTime:MMM d, h:mm tt}", Width = 440,
                VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Opacity = task.IsDone ? 0.55 : 1 });
            row.Children.Add(Button("Remove", () => { settings.Tasks.RemoveAll(x => x.Id == task.Id); Save(); RefreshTasks(); }));
            tasks.Children.Add(row);
        }
        if (settings.Tasks.Count == 0) tasks.Children.Add(new TextBlock { Text = "No tasks added yet.", Opacity = 0.6 });
    }
    private void RefreshMeetings()
    {
        meetings.Children.Clear();
        foreach (var item in settings.Meetings.Where(x => x.StartsAt > DateTimeOffset.Now).OrderBy(x => x.StartsAt).ToList())
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            row.Children.Add(new TextBlock { Text = $"{item.StartsAt.LocalDateTime:MMM d, h:mm tt}  ·  {item.Title}", Width = 470,
                VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis });
            row.Children.Add(Button("Remove", () => { settings.Meetings.RemoveAll(x => x.Id == item.Id); Save(); RefreshMeetings(); }));
            meetings.Children.Add(row);
        }
        if (meetings.Children.Count == 0) meetings.Children.Add(new TextBlock { Text = "No upcoming manual meetings.", Opacity = 0.6 });
    }
    public void SetFeedStatus(string text) => feedStatus.Text = text;
}
