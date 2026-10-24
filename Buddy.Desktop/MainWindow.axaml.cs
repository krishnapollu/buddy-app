using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Buddy.Desktop;

public partial class MainWindow : Window
{
    private readonly BuddySettings settings;
    private readonly TextBlock face;
    private readonly TextBlock bubble;
    private readonly DispatcherTimer alertTimer = new() { Interval = TimeSpan.FromSeconds(6) };
    private bool movingForAlert;
    private bool positionReady;
    private readonly DispatcherTimer motionTimer = new() { Interval = TimeSpan.FromMilliseconds(30) };
    private int motionStep;
    private PixelPoint motionStart;
    private PixelPoint motionTarget;
    private PixelPoint homePosition;
    public event Action? OpenSettingsRequested;
    public MainWindow(BuddySettings settings)
    {
        this.settings = settings;
        InitializeComponent();
        var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Spacing = 2 };
        bubble = new TextBlock { IsVisible = false, Background = Brushes.White, Foreground = Brushes.Black,
            FontSize = 13, Padding = new Thickness(10, 6), TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap, MaxWidth = 185, HorizontalAlignment = HorizontalAlignment.Center };
        face = new TextBlock { FontSize = 92, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center };
        face.PointerPressed += (_, e) => { if (e.GetCurrentPoint(face).Properties.IsLeftButtonPressed) BeginMoveDrag(e); };
        face.DoubleTapped += (_, _) => OpenSettingsRequested?.Invoke();
        var controls = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 8 };
        var settingsButton = new Button { Content = "⚙", FontSize = 15, Padding = new Thickness(8, 2) };
        settingsButton.Click += (_, _) => OpenSettingsRequested?.Invoke();
        var hideButton = new Button { Content = "×", FontSize = 15, Padding = new Thickness(8, 2) };
        hideButton.Click += (_, _) => { settings.Visible = false; SettingsStore.Save(settings); Hide(); };
        controls.Children.Add(settingsButton); controls.Children.Add(hideButton);
        panel.Children.Add(bubble); panel.Children.Add(face); panel.Children.Add(controls);
        Content = panel;
        RefreshAvatar();
        Opened += (_, _) => {
            if (settings.X.HasValue && settings.Y.HasValue && Screens.All.Any(screen =>
                settings.X.Value >= screen.WorkingArea.X && settings.X.Value <= screen.WorkingArea.Right - 80 &&
                settings.Y.Value >= screen.WorkingArea.Y && settings.Y.Value <= screen.WorkingArea.Bottom - 80))
                Position = new PixelPoint((int)settings.X.Value, (int)settings.Y.Value);
            else ResetPosition();
            positionReady = true;
            settings.X = Position.X; settings.Y = Position.Y; SettingsStore.Save(settings);
        };
        PositionChanged += (_, _) => {
            if (movingForAlert || !positionReady) return;
            settings.X = Position.X; settings.Y = Position.Y; SettingsStore.Save(settings);
        };
        motionTimer.Tick += (_, _) => {
            motionStep++;
            var fraction = Math.Min(1.0, motionStep / 15.0);
            Position = new PixelPoint((int)(motionStart.X + (motionTarget.X - motionStart.X) * fraction), (int)(motionStart.Y + (motionTarget.Y - motionStart.Y) * fraction));
            if (motionStep >= 15) { motionTimer.Stop(); if (motionTarget == homePosition) { movingForAlert = false; settings.X = Position.X; settings.Y = Position.Y; SettingsStore.Save(settings); } }
        };
        alertTimer.Tick += (_, _) => { alertTimer.Stop(); bubble.IsVisible = false; if (movingForAlert) MoveTo(homePosition); };
    }
    public void RefreshAvatar()
    {
        if (!string.IsNullOrEmpty(settings.ImagePath) && File.Exists(settings.ImagePath))
        {
            try {
                var image = new Image { Source = new Bitmap(settings.ImagePath), Width = 110, Height = 110, Stretch = Stretch.Uniform };
                var panel = (StackPanel)Content!;
                panel.Children[1] = image;
                return;
            } catch (IOException) { }
        }
        var root = (StackPanel)Content!;
        root.Children[1] = face;
        face.Text = Avatars.Emoji(settings.Avatar);
    }
    public void ShowStatus(string message)
    {
        if (!IsVisible) return;
        bubble.Text = message; bubble.IsVisible = true;
        alertTimer.Stop(); alertTimer.Start();
    }
    public void Alert(string message)
    {
        if (!IsVisible) { settings.Visible = true; Show(); }
        bubble.Text = message; bubble.IsVisible = true;
        if (settings.MoveForReminders && !movingForAlert)
        {
            homePosition = Position; movingForAlert = true;
            var screen = Screens.ScreenFromWindow(this)?.WorkingArea ?? Screens.Primary?.WorkingArea;
            if (screen.HasValue)
            {
                var area = screen.Value;
                var targetX = Math.Clamp(Position.X + 150, area.X, area.Right - 190);
                var targetY = Math.Clamp(Position.Y - 90, area.Y, area.Bottom - 205);
                MoveTo(new PixelPoint(targetX, targetY));
            }
        }
        alertTimer.Stop(); alertTimer.Start();
        if (settings.Speak) Speech.Say(message);
    }
    private void MoveTo(PixelPoint target)
    {
        motionTimer.Stop(); motionStart = Position; motionTarget = target; motionStep = 0; motionTimer.Start();

    }
    public void ResetPosition()
    {
        var area = Screens.Primary?.WorkingArea;
        if (!area.HasValue) return;
        Position = new PixelPoint(area.Value.Right - 220, area.Value.Bottom - 235);
    }
}
