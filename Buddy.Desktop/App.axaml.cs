using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Buddy.Desktop;

public partial class App : Application
{
    private readonly BuddySettings settings = SettingsStore.Load();
    private readonly CalendarFeed feed = new();
    private List<Meeting> calendarMeetings = [];
    private MainWindow? companion;
    private SettingsWindow? preferences;
    private DateTimeOffset lastFeedRefresh = DateTimeOffset.MinValue;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(30) };
    private readonly DispatcherTimer hookTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            companion = new MainWindow(settings);
            companion.OpenSettingsRequested += ShowSettings;
            SetupTray(desktop);
            desktop.MainWindow = companion;
            if (!settings.Visible) companion.Opened += (_, _) => companion.Hide();
            timer.Tick += async (_, _) => await Tick();
            timer.Start();
            hookTimer.Tick += (_, _) => {
                foreach (var message in AgentHooks.Drain(settings)) companion?.ShowStatus(message);
            };
            hookTimer.Start();
            Dispatcher.UIThread.Post(async () => await Tick());
        }
        base.OnFrameworkInitializationCompleted();
    }
    private void SetupTray(IClassicDesktopStyleApplicationLifetime desktop)
    {
        using var stream = AssetLoader.Open(new Uri("avares://Buddy.Desktop/Assets/paw.png"));
        var menu = new NativeMenu();
        var show = new NativeMenuItem("Show Buddy");
        show.Click += (_, _) => { settings.Visible = true; companion?.Show(); SettingsStore.Save(settings); };
        var hide = new NativeMenuItem("Hide Buddy");
        hide.Click += (_, _) => { settings.Visible = false; companion?.Hide(); SettingsStore.Save(settings); };
        var preferencesItem = new NativeMenuItem("Settings…");
        preferencesItem.Click += (_, _) => ShowSettings();
        var quit = new NativeMenuItem("Quit Buddy");
        quit.Click += (_, _) => desktop.Shutdown();
        menu.Items.Add(show); menu.Items.Add(hide); menu.Items.Add(preferencesItem); menu.Items.Add(new NativeMenuItemSeparator()); menu.Items.Add(quit);
        var tray = new TrayIcon { Icon = new WindowIcon(stream), ToolTipText = "Buddy", Menu = menu };
        TrayIcon.SetIcons(this, new TrayIcons { tray });
    }
    private async Task Tick()
    {
        if (DateTimeOffset.Now - lastFeedRefresh >= TimeSpan.FromMinutes(5))
        {
            lastFeedRefresh = DateTimeOffset.Now;
            try { calendarMeetings = await feed.FetchAsync(settings.IcsUrl); preferences?.SetFeedStatus($"Calendar synced: {calendarMeetings.Count} upcoming events"); }
            catch (Exception e)
            { preferences?.SetFeedStatus("Calendar error: " + e.Message); }
        }
        var now = DateTimeOffset.Now;
        var alerts = ReminderEngine.Due(settings, calendarMeetings, now);
        foreach (var alert in alerts)
        {
            settings.LastAlerts[alert.Key] = now;

        }
        if (alerts.Count > 0) companion?.Alert(string.Join(". ", alerts.Select(a => a.Text)));
        foreach (var key in settings.LastAlerts.Where(x => x.Key.StartsWith("meeting:") && now - x.Value > TimeSpan.FromDays(2)).Select(x => x.Key).ToList()) settings.LastAlerts.Remove(key);
        SettingsStore.Save(settings);
    }
    private void ShowSettings()
    {
        preferences ??= new SettingsWindow(settings, () => {
            companion?.RefreshAvatar();
        }, () => {
            lastFeedRefresh = DateTimeOffset.MinValue;
            Dispatcher.UIThread.Post(async () => await Tick());
        }, () => {
            settings.Visible = true; companion?.Show(); companion?.Activate(); SettingsStore.Save(settings);
        }, () => { companion?.ResetPosition(); }, () => { if (preferences is not null) preferences.AllowClose = true; (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown(); });
        preferences.Show(); preferences.Activate();
    }
}
