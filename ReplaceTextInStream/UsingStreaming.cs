using System.Buffers;

namespace ReplaceTextInStream;

public abstract class UsingStreaming : IStreamingReplacer
{
    public abstract Task Replace(Stream input, Stream output, string oldValue, string newValue,
        CancellationToken cancellationToken = default);

    protected bool FindCandidate(out ReadOnlySequence<char> before,
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

    protected bool CompareSequence(ref SequenceReader<char> reader, string oldValue)
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