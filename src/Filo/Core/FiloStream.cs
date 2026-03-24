namespace ManuHub.Filo;

public class FiloStream : Stream
{
    private readonly FiloReader _reader;
    private readonly string _fileName;
    private readonly byte[]? _key;

    private IAsyncEnumerator<byte[]>? _chunks;

    private byte[]? _currentChunk;
    private int _chunkPosition;

    private bool _initialized;

    public FiloStream(FiloReader reader, string fileName, byte[]? key = null)
    {
        _reader = reader;
        _fileName = fileName;
        _key = key;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    private async Task EnsureInitializedAsync()
    {
        if (_initialized)
            return;

        _chunks = _reader.StreamFileAsync(_fileName, _key).GetAsyncEnumerator();
        _initialized = true;

        await MoveNextChunkAsync();
    }

    private async Task MoveNextChunkAsync()
    {
        if (_chunks == null)
            return;

        if (await _chunks.MoveNextAsync())
        {
            _currentChunk = _chunks.Current;
            _chunkPosition = 0;
        }
        else
        {
            _currentChunk = null;
        }
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync();

        if (_currentChunk == null)
            return 0;

        int totalRead = 0;

        while (count > 0 && _currentChunk != null)
        {
            int remaining = _currentChunk.Length - _chunkPosition;

            if (remaining <= 0)
            {
                await MoveNextChunkAsync();
                continue;
            }

            int toCopy = Math.Min(count, remaining);

            Buffer.BlockCopy(_currentChunk, _chunkPosition, buffer, offset, toCopy);

            _chunkPosition += toCopy;
            offset += toCopy;
            count -= toCopy;
            totalRead += toCopy;
        }

        return totalRead;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer, offset, count)
            .GetAwaiter()
            .GetResult();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_chunks != null)
            {
                _chunks.DisposeAsync()
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
            }
        }

        base.Dispose(disposing);
    }
}