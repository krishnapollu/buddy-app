using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Buddy.Desktop;

public sealed record AvatarOption(string Category, string Name, string Atlas, int Index, int Frames = 1,
    string? BetweenAtlas = null, string? GestureAtlas = null);

public static class Avatars
{
    public static readonly AvatarOption[] All = [
        new("Pets", "Cat", "pets", 0), new("Pets", "Dog", "pets", 1),
        new("Pets", "Rabbit", "pets", 2), new("Pets", "Fox", "pets", 3),
        new("Pets", "Panda", "pets", 4), new("Pets", "Owl", "pets", 5),
        new("Fantasy", "Dragon", "pets", 6), new("Fantasy", "Unicorn", "pets", 7),
        new("Cartoons", "Robot", "characters", 0), new("Cartoons", "Ghost", "characters", 1),
        new("Cartoons", "Cloud", "characters", 2), new("Fantasy", "Alien", "characters", 3),
        new("Cinema", "Star", "characters", 4), new("Cinema", "Director", "characters", 5),
        new("Cinema", "Detective", "characters", 6), new("Cinema", "Hero", "characters", 7),
        new("Cinema", "Mohanlal", "mohanlal-walk", 0, 8, "mohanlal-between", "mohanlal-gesture"),
        new("Cinema", "Vijay", "vijay-walk", 0, 8, "vijay-between", "vijay-gesture")
    ];
    private static readonly Dictionary<string, Bitmap> Atlases = new();
    public static AvatarOption Find(string name) => All.FirstOrDefault(x => x.Name == name) ?? All[0];
    public static Bitmap Atlas(string name)
    {
        if (Atlases.TryGetValue(name, out var bitmap)) return bitmap;
        using var stream = AssetLoader.Open(new Uri($"avares://Buddy.Desktop/Assets/{name}-atlas.png"));
        bitmap = new Bitmap(stream);
        Atlases[name] = bitmap;
        return bitmap;
    }
}

public sealed class AvatarView : Control
{
    private readonly AvatarOption avatar;
    private int frame;
    private int gestureFrame = -1;
    public bool HasWalkFrames => avatar.Frames > 1;
    public bool HasGestures => avatar.GestureAtlas is not null;
    public int FrameCount => avatar.Frames;
    public int Frame
    {
        get => frame;
        set { if (frame == value) return; frame = value; InvalidateVisual(); }
    }
    public int GestureFrame
    {
        get => gestureFrame;
        set { if (gestureFrame == value) return; gestureFrame = value; InvalidateVisual(); }
    }
    public AvatarView(AvatarOption avatar)
    {
        this.avatar = avatar;
        gestureFrame = avatar.GestureAtlas is null ? -1 : 0;
        Width = 100; Height = 100;
    }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var atlas = gestureFrame >= 0 && avatar.GestureAtlas is not null ? avatar.GestureAtlas :
            avatar.BetweenAtlas is not null && frame % 2 == 1 ? avatar.BetweenAtlas : avatar.Atlas;
        var bitmap = Avatars.Atlas(atlas);
        var cellWidth = bitmap.PixelSize.Width / 4.0;
        var cellHeight = bitmap.PixelSize.Height / (avatar.Frames > 1 ? 1.0 : 2.0);
        var column = gestureFrame >= 0 && avatar.GestureAtlas is not null ? gestureFrame :
            avatar.Frames == 1 ? avatar.Index % 4 : avatar.BetweenAtlas is null ? frame % 4 : frame / 2;
        var source = new Rect(column * cellWidth,
            (avatar.Frames > 1 ? 0 : avatar.Index / 4) * cellHeight, cellWidth, cellHeight);
        if (avatar.Frames == 1) { context.DrawImage(bitmap, source, Bounds); return; }
        var scale = Math.Min(Bounds.Width / cellWidth, Bounds.Height / cellHeight);
        var destination = new Rect(Bounds.X + (Bounds.Width - cellWidth * scale) / 2,
            Bounds.Y + (Bounds.Height - cellHeight * scale) / 2, cellWidth * scale, cellHeight * scale);
        context.DrawImage(bitmap, source, destination);
    }
}
