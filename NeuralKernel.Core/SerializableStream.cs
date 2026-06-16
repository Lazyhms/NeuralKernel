namespace NeuralKernel.Core;

[Serializable]
public sealed class SerializableStream(Stream innerStream) : Stream
{
    public override bool CanRead => innerStream.CanRead;

    public override bool CanSeek => innerStream.CanSeek;

    public override bool CanWrite => innerStream.CanWrite;

    public override long Length => innerStream.Length;

    public override long Position
    {
        get => innerStream.Position;
        set => innerStream.Position = value;
    }

    public override int ReadTimeout { get; set; } = Timeout.Infinite;

    public override int WriteTimeout { get; set; } = Timeout.Infinite;

    public override bool CanTimeout => innerStream.CanTimeout;

    public override void Flush() => innerStream.Flush();

    public override int Read(byte[] buffer, int offset, int count) => innerStream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) => innerStream.Seek(offset, origin);

    public override void SetLength(long value) => innerStream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count) => innerStream.Write(buffer, offset, count);

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => await innerStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => await innerStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);

    public override async Task FlushAsync(CancellationToken cancellationToken)
        => await innerStream.FlushAsync(cancellationToken);

    protected override void Dispose(bool disposing) => innerStream.Dispose();

    public override ValueTask DisposeAsync() => innerStream.DisposeAsync();

    public override bool Equals(object? obj) => innerStream.Equals(obj);

    public override int GetHashCode() => innerStream.GetHashCode();

    public override string? ToString() => innerStream.ToString();

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => innerStream.BeginRead(buffer, offset, count, callback, state);

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => innerStream.BeginWrite(buffer, offset, count, callback, state);

    public override void Close() => innerStream.Close();

    public override void CopyTo(Stream destination, int bufferSize) => innerStream.CopyTo(destination, bufferSize);

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => innerStream.CopyToAsync(destination, bufferSize, cancellationToken);

    public override int EndRead(IAsyncResult asyncResult) => innerStream.EndRead(asyncResult);

    public override void EndWrite(IAsyncResult asyncResult) => innerStream.EndWrite(asyncResult);

    public override int Read(Span<byte> buffer) => innerStream.Read(buffer);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => innerStream.ReadAsync(buffer, cancellationToken);

    public override int ReadByte() => innerStream.ReadByte();

    public override void Write(ReadOnlySpan<byte> buffer) => innerStream.Write(buffer);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => innerStream.WriteAsync(buffer, cancellationToken);

    public override void WriteByte(byte value) => innerStream.WriteByte(value);
}
