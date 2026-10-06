namespace KaedePhi.Tool.App.Gui.Services;

internal sealed class ProgressReadStream : Stream
{
    private const double ProgressReportInterval = 0.01;
    private readonly Stream _inner;
    private readonly IProgress<double?> _progress;
    private readonly long _length;
    private double _lastReportedProgress;
    private bool _hasReportedEnd;

    public ProgressReadStream(Stream inner, IProgress<double?> progress)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        _length = _inner.CanSeek ? _inner.Length : 0;
        _progress.Report(0);
    }

    public override bool CanRead => _inner.CanRead;

    public override bool CanSeek => _inner.CanSeek;

    public override bool CanWrite => _inner.CanWrite;

    public override long Length => _inner.Length;

    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override void Flush() => _inner.Flush();

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _inner.Read(buffer, offset, count);
        ReportProgress(bytesRead, count);
        return bytesRead;
    }

    public override int Read(Span<byte> buffer)
    {
        var bytesRead = _inner.Read(buffer);
        ReportProgress(bytesRead, buffer.Length);
        return bytesRead;
    }

    public override int ReadByte()
    {
        var value = _inner.ReadByte();
        ReportProgress(value < 0 ? 0 : 1, 1);
        return value;
    }

    public override async Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    )
    {
        var bytesRead = await _inner
            .ReadAsync(buffer, offset, count, cancellationToken)
            .ConfigureAwait(false);
        ReportProgress(bytesRead, count);
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default
    )
    {
        var bytesRead = await _inner
            .ReadAsync(buffer, cancellationToken)
            .ConfigureAwait(false);
        ReportProgress(bytesRead, buffer.Length);
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

    public override void SetLength(long value) => _inner.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count) =>
        _inner.Write(buffer, offset, count);

    public override void Write(ReadOnlySpan<byte> buffer) => _inner.Write(buffer);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _inner.Dispose();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _inner.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private void ReportProgress(int bytesRead, int requestedBytes)
    {
        if (requestedBytes == 0)
            return;

        if (bytesRead == 0 || _length <= 0 || _inner.Position >= _length)
        {
            if (!_hasReportedEnd)
            {
                _hasReportedEnd = true;
                _progress.Report(null);
            }

            return;
        }

        var currentProgress = (double)_inner.Position / _length;
        if (currentProgress - _lastReportedProgress < ProgressReportInterval)
            return;

        _lastReportedProgress = currentProgress;
        _progress.Report(currentProgress);
    }
}
