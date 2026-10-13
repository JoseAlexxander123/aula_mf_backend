namespace AulaVirtual.SharedKernel.Infrastructure.Storage;

internal sealed class BoundedReadStream(Stream inner, long maximumBytes) : Stream
{
    public long BytesRead { get; private set; }
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => BytesRead; set => throw new NotSupportedException(); }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = inner.Read(buffer, offset, count);
        Count(read);
        return read;
    }

    public override int Read(Span<byte> buffer)
    {
        var read = inner.Read(buffer);
        Count(read);
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await inner.ReadAsync(buffer, cancellationToken);
        Count(read);
        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var read = await inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
        Count(read);
        return read;
    }

    private void Count(int read)
    {
        BytesRead += read;
        if (BytesRead > maximumBytes)
            throw new InvalidDataException("Uploaded stream exceeded its declared size.");
    }

    protected override void Dispose(bool disposing) { base.Dispose(disposing); }
    public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
