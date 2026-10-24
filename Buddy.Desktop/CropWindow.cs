using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace Buddy.Desktop;

public sealed class CropWindow : Window
{
    private readonly SKBitmap source;
    private readonly CropPreview preview;
    private readonly TextBlock error = new() { Foreground = Brushes.IndianRed, TextWrapping = TextWrapping.Wrap };

    public CropWindow(byte[] data)
    {
        using var stream = new MemoryStream(data);
        source = SKBitmap.Decode(stream) ?? throw new InvalidDataException("This image could not be opened. Try a PNG or JPEG.");
        if (source.Width > 6000 || source.Height > 6000) throw new InvalidDataException("Choose an image smaller than 6000 × 6000 pixels.");
        using var previewStream = new MemoryStream(data);
        var display = new Bitmap(previewStream);
        preview = new CropPreview(display, source.Width, source.Height) { Width = 250, Height = 250, HorizontalAlignment = HorizontalAlignment.Center };
        Title = "Crop avatar"; Width = 440; Height = 540; MinWidth = 400; MinHeight = 500;
        var body = new StackPanel { Margin = new Thickness(20), Spacing = 10 };
        body.Children.Add(new TextBlock { Text = "Crop your avatar", FontSize = 24, FontWeight = FontWeight.Bold });
        body.Children.Add(new TextBlock { Text = "Drag to position the image. Adjust zoom, then save the square crop.", TextWrapping = TextWrapping.Wrap });
        body.Children.Add(preview);
        var slider = new Slider { Minimum = 1, Maximum = 3, Value = 1, Width = 310 };
        slider.ValueChanged += (_, _) => preview.Zoom = slider.Value;
        body.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 8,
            Children = { new TextBlock { Text = "Zoom", VerticalAlignment = VerticalAlignment.Center }, slider } });
        body.Children.Add(error);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        var cancel = new Button { Content = "Cancel" };
        cancel.Click += (_, _) => Close(null);
        var save = new Button { Content = "Save avatar" };
        save.Click += (_, _) => SaveCrop();
        buttons.Children.Add(cancel); buttons.Children.Add(save); body.Children.Add(buttons);
        Content = body;
        Closed += (_, _) => { source.Dispose(); display.Dispose(); };
    }

    private void SaveCrop()
    {
        try
        {
            var crop = preview.SourceRect;
            using var output = new SKBitmap(512, 512);
            using (var canvas = new SKCanvas(output))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.DrawBitmap(source, new SKRect((float)crop.X, (float)crop.Y, (float)crop.Right, (float)crop.Bottom),
                    new SKRect(0, 0, 512, 512));
            }
            using var image = SKImage.FromBitmap(output);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 95);
            Directory.CreateDirectory(SettingsStore.DirectoryPath);
            var path = Path.Combine(SettingsStore.DirectoryPath, "avatar-custom.png");
            var temporary = path + ".tmp";
            using (var file = File.Create(temporary)) encoded.SaveTo(file);
            File.Move(temporary, path, true);
            Close(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException)
        { error.Text = "Could not save the cropped image: " + e.Message; }
    }
}

public sealed class CropPreview : Control
{
    private readonly Bitmap image;
    private readonly int imageWidth;
    private readonly int imageHeight;
    private double zoom = 1;
    private double centerX = 0.5;
    private double centerY = 0.5;
    private Point? dragStart;
    public CropPreview(Bitmap image, int imageWidth, int imageHeight)
    { this.image = image; this.imageWidth = imageWidth; this.imageHeight = imageHeight; ClipToBounds = true; }
    public double Zoom { get => zoom; set { zoom = value; InvalidateVisual(); } }
    public Rect SourceRect
    {
        get
        {
            var side = Math.Min(imageWidth, imageHeight) / zoom;
            var x = Math.Clamp(centerX * imageWidth - side / 2, 0, imageWidth - side);
            var y = Math.Clamp(centerY * imageHeight - side / 2, 0, imageHeight - side);
            return new Rect(x, y, side, side);
        }
    }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawImage(image, SourceRect, Bounds);
        context.DrawRectangle(null, new Pen(Brushes.White, 2), Bounds.Deflate(1));
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    { base.OnPointerPressed(e); dragStart = e.GetPosition(this); e.Pointer.Capture(this); }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (dragStart is null) return;
        var current = e.GetPosition(this);
        var side = SourceRect.Width;
        centerX -= (current.X - dragStart.Value.X) * side / (Math.Max(1, Bounds.Width) * imageWidth);
        centerY -= (current.Y - dragStart.Value.Y) * side / (Math.Max(1, Bounds.Height) * imageHeight);
        centerX = Math.Clamp(centerX, 0, 1); centerY = Math.Clamp(centerY, 0, 1);
        dragStart = current; InvalidateVisual();
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    { base.OnPointerReleased(e); dragStart = null; e.Pointer.Capture(null); }
}
