namespace DevToolkit.Infrastructure.Helpers;

internal sealed class LimitedMemoryStream : MemoryStream
{
    private readonly int _limit;

    public LimitedMemoryStream(int limit) : base(Math.Min(limit, 4096))
    {
        _limit = limit;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        CheckLength(count);
        base.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        CheckLength(buffer.Length);
        base.Write(buffer);
    }

    public override void WriteByte(byte value)
    {
        CheckLength(1);
        base.WriteByte(value);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        CheckLength(count);

        return base.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        CheckLength(buffer.Length);

        return base.WriteAsync(buffer, cancellationToken);
    }

    public override void SetLength(long value)
    {
        if (value > _limit)
        {
            throw new IOException("The response exceeds the configured retry protection limit.");
        }

        base.SetLength(value);
    }

    private void CheckLength(int count)
    {
        if (Position > _limit - count)
        {
            throw new IOException("The response exceeds the configured retry protection limit.");
        }
    }
}
