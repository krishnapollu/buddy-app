using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
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
    private readonly Grid avatarHost;
    private readonly Grid avatarVisual;
    private AvatarView? activeAvatar;
    private readonly ScaleTransform avatarScale = new();
    private readonly RotateTransform avatarRotation = new();
    private readonly TranslateTransform avatarLift = new();
    private readonly ScaleTransform shadowScale = new();
    private readonly DispatcherTimer alertTimer = new() { Interval = TimeSpan.FromSeconds(6) };
    private readonly DispatcherTimer motionTimer = new() { Interval = TimeSpan.FromMilliseconds(30) };
    private bool movingForAlert;
    private bool positionReady;
    private int motionStep;
    private int motionSteps;
    private int motionHops;
    private PixelPoint motionStart;
    private PixelPoint motionTarget;
    private PixelPoint homePosition;
    private Action? motionFinished;
    private string pendingAnnouncement = "";

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
        avatarHost = new Grid { Width = 155, Height = 155 };
        avatarVisual = new Grid { Width = 155, Height = 155, RenderTransformOrigin = RelativePoint.Center,
            RenderTransform = new TransformGroup { Children = { avatarScale, avatarRotation, avatarLift } } };
        var shadow = new Ellipse { Width = 70, Height = 8, Fill = new SolidColorBrush(Color.FromArgb(45, 0, 0, 0)),
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 4), RenderTransform = shadowScale };
        avatarHost.Children.Add(shadow);
        avatarHost.Children.Add(avatarVisual);
        panel.Children.Add(avatarHost);
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
            var (fraction, lift, phase) = HopFrame(motionStep, motionSteps, motionHops);
            if (activeAvatar?.HasWalkFrames == true) fraction = Math.Min(1.0, (double)motionStep / motionSteps);
            Position = new PixelPoint(
                (int)Math.Round(motionStart.X + (motionTarget.X - motionStart.X) * fraction),
                (int)Math.Round(motionStart.Y + (motionTarget.Y - motionStart.Y) * fraction));
            if (activeAvatar?.HasWalkFrames == true)
            {
                activeAvatar.Frame = ((motionStep - 1) / 4) % 4;
                avatarLift.Y = -2 * Math.Abs(Math.Sin(2 * Math.PI * phase));
                avatarRotation.Angle = 0;
                avatarScale.ScaleX = motionTarget.X < motionStart.X ? -1 : 1;
                avatarScale.ScaleY = 1;
                shadowScale.ScaleX = shadowScale.ScaleY = 1;
            }
            else
            {
                avatarLift.Y = -17 * lift;
                avatarRotation.Angle = Math.Sign(motionTarget.X - motionStart.X) * 5 * Math.Sin(2 * Math.PI * phase);
                avatarScale.ScaleX = 1 + 0.04 * lift;
                avatarScale.ScaleY = 1 - 0.07 * lift;
                shadowScale.ScaleX = 1 - 0.35 * lift;
                shadowScale.ScaleY = 1 - 0.2 * lift;
            }
            if (motionStep >= motionSteps) {
                motionTimer.Stop();
                avatarLift.Y = 0; avatarRotation.Angle = 0;
                avatarScale.ScaleX = 1; avatarScale.ScaleY = 1;
                shadowScale.ScaleX = 1; shadowScale.ScaleY = 1;
                if (activeAvatar is not null) activeAvatar.Frame = 0;
                if (motionTarget == homePosition) {
                    movingForAlert = false;
                    settings.X = Position.X; settings.Y = Position.Y; SettingsStore.Save(settings);
                }
                var complete = motionFinished;
                motionFinished = null;
                complete?.Invoke();
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
                activeAvatar = null;
                SetAvatar(new Image { Source = new Bitmap(stream), Width = 155, Height = 155, Stretch = Stretch.Uniform });
                return;
            } catch (Exception) { }
        }
        activeAvatar = new AvatarView(Avatars.Find(settings.Avatar)) { Width = 155, Height = 155 };
        SetAvatar(activeAvatar);
    }
    private void SetAvatar(Control avatar)
    {
        avatarVisual.Children.Clear();
        avatarVisual.Children.Add(avatar);
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
        pendingAnnouncement = message;
        alertTimer.Stop();
        if (settings.MoveForReminders)
        {
            if (!movingForAlert) homePosition = Position;
            var screen = Screens.ScreenFromWindow(this)?.WorkingArea ?? Screens.Primary?.WorkingArea;
            if (screen.HasValue)
            {
                movingForAlert = true;
                bubble.IsVisible = false;
                MoveTo(ReminderTarget(screen.Value, Position, settings.ReminderMotion), Announce);
                return;
            }
        }
        Announce();
    }
    private void Announce()
    {
        bubble.Text = pendingAnnouncement; bubble.IsVisible = true;
        alertTimer.Stop(); alertTimer.Start();
        if (settings.Speak) Speech.Say(pendingAnnouncement, settings.Voice);
    }
    internal static PixelPoint ReminderTarget(PixelRect area, PixelPoint current, string mode)
    {
        var centerX = area.X + (area.Width - 190) / 2;
        var centerY = area.Y + (area.Height - 190) / 2;
        var x = mode == "Cross" ? (current.X >= centerX ? area.X + 20 : area.Right - 210) : centerX;
        return new PixelPoint(Math.Clamp(x, area.X, Math.Max(area.X, area.Right - 190)),
            Math.Clamp(centerY, area.Y, Math.Max(area.Y, area.Bottom - 190)));
    }
    internal static (double Fraction, double Lift, double Phase) HopFrame(int step, int steps, int hops)
    {
        var progress = Math.Min(1.0, (double)step / steps);
        var hop = progress * hops;
        var phase = progress >= 1 ? 0 : hop - Math.Floor(hop);
        return ((Math.Floor(hop) + phase * phase * (3 - 2 * phase)) / hops,
            Math.Sin(Math.PI * phase), phase);
    }
    private void MoveTo(PixelPoint target, Action? complete = null)
    {
        motionTimer.Stop(); motionStart = Position; motionTarget = target; motionStep = 0;
        motionFinished = complete;
        var distance = Math.Sqrt(Math.Pow(target.X - Position.X, 2) + Math.Pow(target.Y - Position.Y, 2));
        motionHops = Math.Max(1, (int)Math.Ceiling(distance / 72));
        motionSteps = motionHops * 14;
        motionTimer.Start();
    }
    public void ResetPosition()
    {
        motionTimer.Stop(); alertTimer.Stop(); movingForAlert = false; motionFinished = null;
        avatarLift.Y = 0; avatarRotation.Angle = 0; avatarScale.ScaleX = avatarScale.ScaleY = 1;
        shadowScale.ScaleX = shadowScale.ScaleY = 1; bubble.IsVisible = false;
        if (activeAvatar is not null) activeAvatar.Frame = 0;
        var area = Screens.Primary?.WorkingArea;
        if (!area.HasValue) return;
        Position = new PixelPoint(area.Value.Right - 220, area.Value.Bottom - 220);
    }
}
