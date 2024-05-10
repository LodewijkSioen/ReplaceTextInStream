using System.Buffers;

namespace ReplaceTextInStream;

public class UsingStreamReader : UsingStreaming
{
    private readonly int _bufferLength;
    
    public UsingStreamReader(int bufferLength = 1024)
    {
        _bufferLength = bufferLength;
    }

    public override async Task Replace(Stream input, Stream output, string oldValue, string newValue, CancellationToken cancellationToken = default)
    {
        var inputBuffer = ArrayPool<char>.Shared.Rent(Math.Max(_bufferLength, oldValue.Length * 2));
        var delimiters = new[]
        {
            char.ToLowerInvariant(oldValue[0]),
            char.ToUpperInvariant(oldValue[0])
        };

        try
        {
            using var reader = new StreamReader(input, leaveOpen: true);
            await using var writer = new StreamWriter(output, reader.CurrentEncoding, leaveOpen: true);
            var startIndex = 0;

            while (true)
            {
                var memory = inputBuffer.AsMemory(startIndex, inputBuffer.Length - startIndex);
                var charactersRead = await reader.ReadBlockAsync(memory, cancellationToken);
                var sequence = new ReadOnlySequence<char>(inputBuffer[..(charactersRead + startIndex)]);
                
                while (sequence.Length >= oldValue.Length)
                {
                    if(FindCandidate(out var before, sequence, oldValue, delimiters, out var position))
                    {
                        await writer.WriteAsync(before.ToArray(), cancellationToken);

                        await writer.WriteAsync(newValue);

                        sequence = sequence.Slice(position);
                    }
                    else
                    {
                        await writer.WriteAsync(before.ToArray(), cancellationToken);
                        sequence = sequence.Slice(position);
                    }
                }

                if (reader.EndOfStream)
                {
                    await writer.WriteAsync(sequence.ToArray(), cancellationToken);
                    break;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                startIndex = (int) sequence.Length;
                Array.Copy(inputBuffer, inputBuffer.Length - startIndex, inputBuffer, 0, sequence.Length);
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(inputBuffer, true);
        }
    }
}