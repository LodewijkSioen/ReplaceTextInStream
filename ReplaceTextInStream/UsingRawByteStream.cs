using System.Buffers;
using System.Text;

namespace ReplaceTextInStream;

public class UsingRawByteStream : IStreamingReplacer
{
    private readonly int _bufferLength;

    public UsingRawByteStream(int bufferLength = 1024)
    {
        _bufferLength = bufferLength;
    }

    public async Task Replace(Stream input, Stream output, string oldValue, string newValue,
        CancellationToken cancellationToken = default)
    {
        var pattern = new Pattern(Encoding.Default, oldValue);
        var inputBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(_bufferLength, pattern.LengthInBytes * 2));
        var newValueInBytes = Encoding.Default.GetBytes(newValue);

        try
        {
            var startIndex = 0;

            while (true)
            {
                var memory = inputBuffer.AsMemory(startIndex, inputBuffer.Length - startIndex);
                var charactersRead = await input.ReadAsync(memory, cancellationToken);
                if (charactersRead == 0)
                {
                    await output.WriteAsync(inputBuffer[..startIndex], cancellationToken);
                    break;
                }

                var sequence = new ReadOnlySequence<byte>(inputBuffer[..(charactersRead + startIndex)]);
                var currentSequenceLength = sequence.Length;

                while (true)
                {
                    if (FindPattern(ref sequence, pattern, out var inspected))
                    {
                        await output.WriteAsync(inspected.ToArray(), cancellationToken);
                        await output.WriteAsync(newValueInBytes, cancellationToken);
                    }
                    else
                    {
                        await output.WriteAsync(inspected.ToArray(), cancellationToken);
                        break;
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                startIndex = (int)sequence.Length;
                Array.Copy(inputBuffer, currentSequenceLength - startIndex, inputBuffer, 0, sequence.Length);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(inputBuffer, true);
        }
    }

    private bool FindPattern(ref ReadOnlySequence<byte> haystack, Pattern pattern, out ReadOnlySequence<byte> inspected)
    {
        var reader = new SequenceReader<byte>(haystack);

        while (true)
        {
            if (reader.TryAdvanceToAny(pattern.Delimiters, false))
            {
                if (reader.Remaining < pattern.LengthInBytes)
                {
                    inspected = haystack.Slice(0, reader.Position);
                    haystack = haystack.Slice(reader.Position);
                    return false;
                }

                var positionOfCandidate = reader.Position;
                if (CompareSequence(ref reader, pattern))
                {
                    inspected = haystack.Slice(0, positionOfCandidate);
                    haystack = haystack.Slice(reader.Position);
                    return true;
                }
            }
            else
            {
                reader.AdvanceToEnd();
                inspected = haystack.Slice(0, reader.Position);
                haystack = haystack.Slice(reader.Position);
                return false;
            }
        }
    }

    private bool CompareSequence(ref SequenceReader<byte> reader, Pattern pattern)
    {
        //We already know that the first byte matches
        reader.Advance(1);

        //Check the rest of the bytes
        for (var i = 1; i < pattern.LengthInBytes; i++)
        {
            if (!reader.IsNext(pattern.UpperBytes[i], true)
                && !reader.IsNext(pattern.LowerBytes[i], true))
            {
                return false;
            }
        }

        return true;
    }
    
    private readonly record struct Pattern
    {
        public Pattern(Encoding encoding, string value)
        {
            UpperBytes = encoding.GetBytes(value.ToUpperInvariant());
            LowerBytes = encoding.GetBytes(value.ToLowerInvariant());
            Delimiters = [LowerBytes[0], UpperBytes[0]];
        }

        public byte[] UpperBytes { get; }
        public byte[] LowerBytes { get; }
        public byte[] Delimiters { get; }
        //This is an assumption: the length in bytes will always be the same if the characters are lowercase or uppercase
        //Unicode does not specify this, but dotnet does not implement the edge cases where it is used.
        public int LengthInBytes => UpperBytes.Length;
    }
}