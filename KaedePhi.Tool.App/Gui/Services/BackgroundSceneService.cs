using Avalonia.Media.Imaging;
using KaedePhi.Tool.App.Gui.Models;

namespace KaedePhi.Tool.App.Gui.Services;

internal sealed class BackgroundSceneService
{
    private const string BackgroundEndpoint = "https://www.loliapi.com/acg/";
    private static readonly HttpClient Client = CreateClient();
    private readonly HttpClient _client;

    public BackgroundSceneService()
        : this(Client) { }

    internal BackgroundSceneService(HttpClient client)
    {
        _client = client;
    }

    public async Task<Bitmap?> FetchAsync(CancellationToken cancellationToken)
    {
        var resource = await FetchResourceAsync(cancellationToken).ConfigureAwait(false);
        return resource?.Image;
    }

    public async Task<BackgroundImageResource?> FetchResourceAsync(
        CancellationToken cancellationToken
    )
    {
        var requestUris = new[]
        {
            $"{BackgroundEndpoint}?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            BackgroundEndpoint,
        };

        foreach (var requestUri in requestUris)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken
                );
                timeout.CancelAfter(TimeSpan.FromSeconds(15));
                using var response = await _client
                    .GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                    .ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    continue;

                var bytes = await response
                    .Content.ReadAsByteArrayAsync(timeout.Token)
                    .ConfigureAwait(false);
                if (bytes.Length == 0)
                    continue;

                using var stream = new MemoryStream(bytes);
                var image = await Task.Run(() => new Bitmap(stream), cancellationToken)
                    .ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested)
                {
                    image.Dispose();
                    return null;
                }

                return new BackgroundImageResource(image, bytes, GetExtension(bytes));
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch
            {
                continue;
            }
        }

        return null;
    }

    public static Task<string> SaveAsync(
        ReadOnlyMemory<byte> imageData,
        string extension,
        CancellationToken cancellationToken,
        string? directory = null
    )
    {
        return Task.Run(
            async () =>
            {
                var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                directory ??= string.IsNullOrEmpty(pictures)
                    ? AppPaths.GetDirectory("Backgrounds")
                    : Path.Combine(pictures, "KaedePhi", "Backgrounds");
                Directory.CreateDirectory(directory);
                var name =
                    $"Background_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}{extension}";
                var path = Path.Combine(directory, name);
                try
                {
                    await using var file = new FileStream(
                        path,
                        new FileStreamOptions
                        {
                            Mode = FileMode.CreateNew,
                            Access = FileAccess.Write,
                            Options = FileOptions.Asynchronous,
                        }
                    );
                    await file.WriteAsync(imageData, cancellationToken).ConfigureAwait(false);
                    return path;
                }
                catch
                {
                    if (File.Exists(path))
                        File.Delete(path);
                    throw;
                }
            },
            cancellationToken
        );
    }

    private static string GetExtension(byte[] bytes)
    {
        if (
            bytes.Length >= 8
            && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
        )
            return ".png";
        if (bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255)
            return ".jpg";
        if (
            bytes.Length >= 12
            && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8)
            && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8)
        )
            return ".webp";
        if (bytes.Length >= 3 && bytes.AsSpan(0, 3).SequenceEqual("GIF"u8))
            return ".gif";
        if (bytes.Length >= 2 && bytes.AsSpan(0, 2).SequenceEqual("BM"u8))
            return ".bmp";
        return ".image";
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("KaedePhi GUI");
        return client;
    }
}
