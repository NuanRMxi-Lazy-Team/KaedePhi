using Avalonia.Media.Imaging;

namespace KaedePhi.Tool.App.Gui.Models;

internal sealed class BackgroundImageResource(Bitmap image, byte[] imageData, string extension)
    : IDisposable
{
    private bool _disposed;

    public Bitmap Image { get; } = image;
    public ReadOnlyMemory<byte> ImageData { get; private set; } = imageData;
    public string Extension { get; } = extension;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Image.Dispose();
        ImageData = ReadOnlyMemory<byte>.Empty;
    }
}
