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
    private readonly Border bubbleCard;
    private readonly TextBlock bubbleTitle;
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
    private readonly DispatcherTimer idleTimer = new() { Interval = TimeSpan.FromMilliseconds(60) };
    private readonly DateTime idleStarted = DateTime.UtcNow;
    private DateTime announcementStarted;
    private bool announcementActive;
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
        bubble = new TextBlock { Foreground = new SolidColorBrush(Color.Parse("#243047")),
            FontSize = 15, FontWeight = FontWeight.SemiBold, TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 260, MaxHeight = 75, HorizontalAlignment = HorizontalAlignment.Center };
        var bubbleContent = new StackPanel { Spacing = 5 };
        bubbleTitle = new TextBlock { Text = "BUDDY REMINDER", FontSize = 10, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#876721")), HorizontalAlignment = HorizontalAlignment.Center };
        bubbleContent.Children.Add(bubbleTitle);
        bubbleContent.Children.Add(bubble);
        bubbleCard = new Border { Child = bubbleContent, IsVisible = false,
            Background = new SolidColorBrush(Color.Parse("#FFF9E9")),
            BorderBrush = new SolidColorBrush(Color.Parse("#E8D29A")), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(13), Padding = new Thickness(13, 10),
            Margin = new Thickness(0, 0, 0, 8), MinWidth = 210, MaxWidth = 290,
            HorizontalAlignment = HorizontalAlignment.Center };
        panel.Children.Add(bubbleCard);
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
            var size = WindowPixelSize();
            var savedScreen = settings.X.HasValue && settings.Y.HasValue ? Screens.All.FirstOrDefault(screen =>
                settings.X.Value >= screen.WorkingArea.X && settings.X.Value < screen.WorkingArea.Right &&
                settings.Y.Value >= screen.WorkingArea.Y && settings.Y.Value < screen.WorkingArea.Bottom) : null;
            if (savedScreen is not null)
            {
                var area = savedScreen.WorkingArea;
                Position = new PixelPoint(Math.Clamp((int)settings.X.GetValueOrDefault(), area.X, Math.Max(area.X, area.Right - size.Width)),
                    Math.Clamp((int)settings.Y.GetValueOrDefault(), area.Y, Math.Max(area.Y, area.Bottom - size.Height)));
            }
            else ResetPosition();
            positionReady = true;
            settings.X = Position.X; settings.Y = Position.Y; SettingsStore.Save(settings);
            idleTimer.Start();
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
                activeAvatar.GestureFrame = -1;
                activeAvatar.Frame = ((motionStep - 1) / 2) % activeAvatar.FrameCount;
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
                if (activeAvatar is not null) { activeAvatar.Frame = 0; activeAvatar.GestureFrame = activeAvatar.HasGestures ? 0 : -1; }
                if (motionTarget == homePosition) {
                    movingForAlert = false;
                    settings.X = Position.X; settings.Y = Position.Y; SettingsStore.Save(settings);
                }
                var complete = motionFinished;
                motionFinished = null;
                complete?.Invoke();
            }
        };
        idleTimer.Tick += (_, _) => {
            if (!IsVisible || motionTimer.IsEnabled) return;
            var seconds = (DateTime.UtcNow - idleStarted).TotalSeconds;
            var breath = Math.Sin(seconds * Math.PI / 2.2);
            avatarScale.ScaleX = 1 + 0.008 * breath;
            avatarScale.ScaleY = 1 - 0.012 * breath;
            var nod = seconds % 11;
            avatarRotation.Angle = 0.8 * Math.Sin(seconds * Math.PI / 3.5) +
                (nod >= 7 && nod < 8.5 ? 1.6 * Math.Sin((nod - 7) * Math.PI / 1.5) : 0);
            avatarLift.Y = -1.5 - 1.5 * Math.Sin(seconds * Math.PI / 2.2);
            if (activeAvatar?.HasGestures == true)
                activeAvatar.GestureFrame = GestureFrameAt(announcementActive ?
                    (DateTime.UtcNow - announcementStarted).TotalSeconds : seconds, announcementActive);
        };
        alertTimer.Tick += (_, _) => {
            alertTimer.Stop(); bubbleCard.IsVisible = false; announcementActive = false;
            if (movingForAlert) MoveTo(homePosition);
        };
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
        announcementActive = false;
        bubbleTitle.Text = "BUDDY STATUS";
        bubble.Text = message; bubbleCard.IsVisible = true;
        alertTimer.Stop(); alertTimer.Start();
    }
    public void Alert(string message)
    {
        if (!IsVisible) { settings.Visible = true; Show(); }
        pendingAnnouncement = message;
        announcementActive = false;
        alertTimer.Stop();
        if (settings.MoveForReminders)
        {
            if (!movingForAlert) homePosition = Position;
            var screen = Screens.ScreenFromWindow(this)?.WorkingArea ?? Screens.Primary?.WorkingArea;
            if (screen.HasValue)
            {
                movingForAlert = true;
                bubbleCard.IsVisible = false;
                MoveTo(ReminderTarget(screen.Value, WindowPixelSize(), (int)Math.Round(155 * RenderScaling)), Announce);
                return;
            }
        }
        Announce();
    }
    private void Announce()
    {
        announcementActive = true;
        announcementStarted = DateTime.UtcNow;
        bubbleTitle.Text = pendingAnnouncement.StartsWith("Task due:", StringComparison.Ordinal) ? "TASK REMINDER" :
            pendingAnnouncement.StartsWith("Meeting soon:", StringComparison.Ordinal) ? "MEETING SOON" : "BUDDY REMINDER";
        bubble.Text = pendingAnnouncement; bubbleCard.IsVisible = true;
        alertTimer.Interval = TimeSpan.FromSeconds(Math.Clamp(pendingAnnouncement.Length / 14.0, 8, 14));
        alertTimer.Stop(); alertTimer.Start();
        if (settings.Speak && activeAvatar?.HasWalkFrames != true) Speech.Say(pendingAnnouncement, settings.Voice);
    }
    private PixelSize WindowPixelSize() => new((int)Math.Ceiling(Width * RenderScaling), (int)Math.Ceiling(Height * RenderScaling));
    internal static PixelPoint ReminderTarget(PixelRect area, PixelSize window, int avatarHeight)
    {
        var x = area.X + (area.Width - window.Width) / 2;
        var y = area.Y + area.Height / 2 - window.Height + avatarHeight / 2;
        return new PixelPoint(Math.Clamp(x, area.X, Math.Max(area.X, area.Right - window.Width)),
            Math.Clamp(y, area.Y, Math.Max(area.Y, area.Bottom - window.Height)));
    }
    internal static (double Fraction, double Lift, double Phase) HopFrame(int step, int steps, int hops)
    {
        var progress = Math.Min(1.0, (double)step / steps);
        var hop = progress * hops;
        var phase = progress >= 1 ? 0 : hop - Math.Floor(hop);
        return ((Math.Floor(hop) + phase * phase * (3 - 2 * phase)) / hops,
            Math.Sin(Math.PI * phase), phase);
    }
    internal static int GestureFrameAt(double seconds, bool announcing)
    {
        var phase = seconds % (announcing ? 4 : 13);
        if (announcing) return phase < 0.25 ? 0 : phase < 0.55 ? 1 : phase < 1.75 ? 3 :
            phase < 2.2 ? 2 : phase < 2.5 ? 1 : 0;
        return phase < 8 || phase >= 9.15 ? 0 : phase < 8.28 ? 1 : phase < 8.85 ? 2 : 1;
    }
    private void MoveTo(PixelPoint target, Action? complete = null)
    {
        if (activeAvatar is not null) activeAvatar.GestureFrame = -1;
        motionTimer.Stop(); motionStart = Position; motionTarget = target; motionStep = 0;
        motionFinished = complete;
        var distance = Math.Sqrt(Math.Pow(target.X - Position.X, 2) + Math.Pow(target.Y - Position.Y, 2));
        motionHops = Math.Max(1, (int)Math.Ceiling(distance / 72));
        motionSteps = motionHops * (activeAvatar?.HasWalkFrames == true ? 16 : 14);
        motionTimer.Start();
    }
    public void ResetPosition()
    {
        motionTimer.Stop(); alertTimer.Stop(); movingForAlert = false; motionFinished = null;
        announcementActive = false;
        avatarLift.Y = 0; avatarRotation.Angle = 0; avatarScale.ScaleX = avatarScale.ScaleY = 1;
        shadowScale.ScaleX = shadowScale.ScaleY = 1; bubbleCard.IsVisible = false;
        if (activeAvatar is not null) { activeAvatar.Frame = 0; activeAvatar.GestureFrame = activeAvatar.HasGestures ? 0 : -1; }
        var area = Screens.Primary?.WorkingArea;
        if (!area.HasValue) return;
        var size = WindowPixelSize();
        Position = new PixelPoint(area.Value.Right - size.Width - 30, area.Value.Bottom - size.Height - 30);
    }
}
