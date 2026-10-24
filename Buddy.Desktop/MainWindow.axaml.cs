using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace Buddy.Desktop;

public partial class MainWindow : Window
{
    private readonly BuddySettings settings;
    private readonly StackPanel panel;
    private readonly TextBlock bubble;
    private readonly DispatcherTimer alertTimer = new() { Interval = TimeSpan.FromSeconds(6) };
    private readonly DispatcherTimer motionTimer = new() { Interval = TimeSpan.FromMilliseconds(30) };
    private bool movingForAlert;
    private bool positionReady;
    private int motionStep;
    private PixelPoint motionStart;
    private PixelPoint motionTarget;
    private PixelPoint homePosition;

    public MainWindow(BuddySettings settings)
    {
        this.settings = settings;
        InitializeComponent();
        panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom };
        panel.PointerPressed += (_, e) => { if (e.GetCurrentPoint(panel).Properties.IsLeftButtonPressed) BeginMoveDrag(e); };
        bubble = new TextBlock { IsVisible = false, Background = Brushes.White, Foreground = Brushes.Black,
            FontSize = 13, Padding = new Thickness(10, 6), TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap, MaxWidth = 190, HorizontalAlignment = HorizontalAlignment.Center };
        panel.Children.Add(bubble);
        panel.Children.Add(new AvatarView(Avatars.Find(settings.Avatar)) { Width = 155, Height = 155 });
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
            if (motionStep >= 15) {
                motionTimer.Stop();
                if (motionTarget == homePosition) {
                    movingForAlert = false;
                    settings.X = Position.X; settings.Y = Position.Y; SettingsStore.Save(settings);
                }
            }
        };
        alertTimer.Tick += (_, _) => { alertTimer.Stop(); bubble.IsVisible = false; if (movingForAlert) MoveTo(homePosition); };
    }

    public void RefreshAvatar()
    {
        if (!string.IsNullOrEmpty(settings.ImagePath) && File.Exists(settings.ImagePath))
        {
            try {
                using var stream = File.OpenRead(settings.ImagePath);
                panel.Children[1] = new Image { Source = new Bitmap(stream), Width = 155, Height = 155, Stretch = Stretch.Uniform };
                return;
            } catch (Exception) { }
        }
        panel.Children[1] = new AvatarView(Avatars.Find(settings.Avatar)) { Width = 155, Height = 155 };
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
                MoveTo(new PixelPoint(Math.Clamp(Position.X + 150, area.X, area.Right - 190),
                    Math.Clamp(Position.Y - 90, area.Y, area.Bottom - 190)));
            }
        }
        alertTimer.Stop(); alertTimer.Start();
        if (settings.Speak) Speech.Say(message, settings.Voice);
    }
    private void MoveTo(PixelPoint target)
    {
        motionTimer.Stop(); motionStart = Position; motionTarget = target; motionStep = 0; motionTimer.Start();
    }
    public void ResetPosition()
    {
        var area = Screens.Primary?.WorkingArea;
        if (!area.HasValue) return;
        Position = new PixelPoint(area.Value.Right - 220, area.Value.Bottom - 220);
    }
}
