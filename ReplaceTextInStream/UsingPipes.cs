using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace ReplaceTextInStream;

public class UsingPipes : IStreamingReplacer
{
    private readonly int _bufferLength;

    public UsingPipes(int bufferLength = 1024)
    {
        _bufferLength = bufferLength;
    }

    public Task Replace(Stream input, Stream output, string oldValue, string newValue,
        CancellationToken cancellationToken = default)
    {
        var pipe = new Pipe(new PipeOptions());

        var reading = ReadIntoStream(pipe.Writer, input, oldValue, newValue, cancellationToken);
        var writing = pipe.Reader.CopyToAsync(output, cancellationToken);

        return Task.WhenAll(reading, writing);
    }

    private async Task ReadIntoStream(PipeWriter writer, Stream input, string oldValue, string newValue, CancellationToken cancellationToken)
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
            var startIndex = 0;

            while (true)
            {
                var memory = inputBuffer.AsMemory(startIndex, inputBuffer.Length - startIndex);
                var charactersRead = await reader.ReadBlockAsync(memory, cancellationToken);
                var sequence = new ReadOnlySequence<char>(inputBuffer[..(charactersRead + startIndex)]);

                while (sequence.Length >= oldValue.Length)
                {
                    if (FindCandidate(out var before, sequence, oldValue, delimiters, out var position))
                    {
                        Encoding.UTF8.GetBytes(before, writer);
                        Encoding.UTF8.GetBytes(newValue, writer);

                        sequence = sequence.Slice(position);
                    }
                    else
                    {
                        Encoding.UTF8.GetBytes(before, writer);
                        sequence = sequence.Slice(position);
                    }
                }

                if (reader.EndOfStream)
                {
                    Encoding.UTF8.GetBytes(sequence, writer);
                    break;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await writer.FlushAsync(cancellationToken);
                startIndex = (int)sequence.Length;
                Array.Copy(inputBuffer, inputBuffer.Length - startIndex, inputBuffer, 0, sequence.Length);
            }
        }
        finally
        {
            await writer.CompleteAsync();
            ArrayPool<char>.Shared.Return(inputBuffer, true);
        }
    }

    private bool FindCandidate(out ReadOnlySequence<char> before,
        ReadOnlySequence<char> haystack,
        string oldValue,
        char[] delimiters,
        out SequencePosition position)
    {
        var reader = new SequenceReader<char>(haystack);

        while (true)
        {
            if (reader.TryReadToAny(out ReadOnlySequence<char> _, delimiters, advancePastDelimiter: false))
            {
                if (reader.Remaining < oldValue.Length)
                {
                    position = reader.Position;
                    before = haystack.Slice(0, reader.Position);
                    return false;
                }

                var positionOfDelimiter = reader.Position;
                if (CompareSequence(ref reader, oldValue))
                {
                    position = reader.Position;
                    before = haystack.Slice(0, positionOfDelimiter);
                    return true;
                }
            }
            else
            {
                reader.AdvanceToEnd();
                position = reader.Position;
                before = haystack;
                return false;
            }
        }
    }

    private bool CompareSequence(ref SequenceReader<char> reader, string oldValue)
    {
        foreach (var character in oldValue)
        {
            if (reader.TryRead(out var candidate) == false
                || Compare(candidate, character) == false)
            {
                return false;
            }
        }

        return true;
    }

    protected bool Compare(char candidate, char original)
    {
        return char.ToUpperInvariant(original) == char.ToUpperInvariant(candidate);
    }
}