using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Buddy.Desktop;

public sealed record AvatarOption(string Category, string Name, string Atlas, int Index);

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
        new("Cinema", "Detective", "characters", 6), new("Cinema", "Hero", "characters", 7)
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
    public AvatarView(AvatarOption avatar) { this.avatar = avatar; Width = 100; Height = 100; }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bitmap = Avatars.Atlas(avatar.Atlas);
        var cellWidth = bitmap.PixelSize.Width / 4.0;
        var cellHeight = bitmap.PixelSize.Height / 2.0;
        var source = new Rect((avatar.Index % 4) * cellWidth, (avatar.Index / 4) * cellHeight, cellWidth, cellHeight);
        context.DrawImage(bitmap, source, Bounds);
    }
}
