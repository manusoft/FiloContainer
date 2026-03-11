namespace ManuHub.Filo;

public class FiloStream : Stream
{
    private readonly FiloReader _reader;
    private readonly string _fileName;
    private readonly byte[]? _key;
    private IAsyncEnumerator<byte[]>? _chunks;
    private MemoryStream? _currentChunk;
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
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    private async Task EnsureInitializedAsync()
    {
        if (!_initialized)
        {
            _chunks = _reader.StreamFileAsync(_fileName, _key).GetAsyncEnumerator();
            _initialized = true;
            await MoveNextChunkAsync();
        }
    }

    private async Task MoveNextChunkAsync()
    {
        if (_chunks != null)
        {
            if (await _chunks.MoveNextAsync())
                _currentChunk = new MemoryStream(_chunks.Current);
            else
                _currentChunk = null;
        }
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync();

        if (_currentChunk == null)
            return 0; // End of stream

        int totalRead = 0;

        while (count > 0 && _currentChunk != null)
        {
            int read = await _currentChunk.ReadAsync(buffer, offset, count, cancellationToken);
            totalRead += read;
            offset += read;
            count -= read;

            if (_currentChunk.Position >= _currentChunk.Length)
                await MoveNextChunkAsync();
        }

        return totalRead;
    }

    protected override void Dispose(bool disposing)
    {
        _chunks?.DisposeAsync().AsTask().Wait();
        _currentChunk?.Dispose();
        base.Dispose(disposing);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }
}