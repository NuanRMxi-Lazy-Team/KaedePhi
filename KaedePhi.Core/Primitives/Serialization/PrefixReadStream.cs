using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KaedePhi.Core.Primitives.Serialization
{
    internal sealed class PrefixReadStream : Stream
    {
        // 底层流归调用方所有，此包装流仅负责重放已探测的前缀。
        private readonly Stream _stream;
        private readonly byte[] _prefix;
        private readonly int _prefixLength;
        private int _prefixPosition;

        internal PrefixReadStream(Stream stream, byte[] prefix, int prefixLength)
        {
            _stream = stream;
            _prefix = prefix;
            _prefixLength = prefixLength;
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) =>
            Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            var prefixCount = ReadPrefix(buffer);
            return prefixCount < buffer.Length
                ? prefixCount + _stream.Read(buffer[prefixCount..])
                : prefixCount;
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default
        )
        {
            var prefixCount = ReadPrefix(buffer.Span);
            return prefixCount == buffer.Length
                ? ValueTask.FromResult(prefixCount)
                : ReadRemainingAsync(buffer, prefixCount, cancellationToken);
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        ) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        private int ReadPrefix(Span<byte> buffer)
        {
            var count = Math.Min(buffer.Length, _prefixLength - _prefixPosition);
            if (count > 0)
            {
                _prefix.AsSpan(_prefixPosition, count).CopyTo(buffer);
                _prefixPosition += count;
            }

            return count;
        }

        private async ValueTask<int> ReadRemainingAsync(
            Memory<byte> buffer,
            int prefixCount,
            CancellationToken cancellationToken
        )
        {
            var remaining = await _stream
                .ReadAsync(buffer[prefixCount..], cancellationToken)
                .ConfigureAwait(false);
            return prefixCount + remaining;
        }
    }
}
