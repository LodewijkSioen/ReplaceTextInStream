using System.Buffers;
using System.Text;

namespace ReplaceTextInStream;

public readonly record struct Pattern
{
    public Pattern(Encoding encoding, string value)
    {
        UpperBytes = encoding.GetBytes(value.ToUpperInvariant());
        LowerBytes = encoding.GetBytes(value.ToLowerInvariant());
        Delimiters = [LowerBytes[0], UpperBytes[0]];
    }

    private byte[] UpperBytes { get; }
    private byte[] LowerBytes { get; }
    private byte[] Delimiters { get; }
    //This is an assumption: the length in bytes will always be the same if the characters are lowercase or uppercase
    //This assumption is actually incorrect, but the characters where this is the case are unlikely for out usecase
    public int MaxLength => UpperBytes.Length;

    /// <summary>
    /// Finds the first occurence of the Pattern in a sequence of bytes
    /// </summary>
    /// <param name="haystack">
    /// The sequence of bytes that might contain the pattern. Passed by reference and will be resliced to
    /// only contain the part of the sequence that has not been inspected. If the pattern is found, the
    /// haystack will be resliced at the end of the pattern.
    /// </param>
    /// <param name="inspected">
    /// The slice of the haystack that has been inpected:
    ///  - If the pattern is found, this will contain all the bytes up to the pattern
    ///  - If the the pattern is not found, this will contain all the bytes of the haystack
    ///  - If the first byte of the pattern is found but there are not enough bytes left in the haystack
    ///    to match the pattern, this will contain all the bytes up to the first byte of the pattern
    /// </param>
    /// <returns>True if the pattern is found in the haystack, otherwise false</returns>
    public bool FindPattern(ref ReadOnlySequence<byte> haystack, out ReadOnlySequence<byte> inspected)
    {
        var reader = new SequenceReader<byte>(haystack);

        while (true)
        {
            if (reader.TryAdvanceToAny(Delimiters, false))
            {
                if (reader.Remaining < MaxLength)
                {
                    inspected = haystack.Slice(0, reader.Position);
                    haystack = haystack.Slice(reader.Position);
                    return false;
                }

                var positionOfCandidate = reader.Position;
                if (CompareSequence(ref reader))
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

    /// <summary>
    /// Compares the next bytes in the reader to the Pattern
    /// </summary>
    /// <param name="reader">
    /// The reader containing the bytes to compare.
    /// Passed by reference because this function will advance the position of the reader to:
    ///  - The last byte of the matching pattern
    ///  - The first byte that doesn't match the pattern
    /// </param>
    /// <returns>True if the pattern matches, otherwise false</returns>
    private bool CompareSequence(ref SequenceReader<byte> reader)
    {
        //We already know that the first byte matches
        reader.Advance(1);

        //Check the rest of the bytes
        for (var i = 1; i < MaxLength; i++)
        {
            if (!reader.IsNext(UpperBytes[i], true)
                && !reader.IsNext(LowerBytes[i], true))
            {
                return false;
            }
        }

        return true;
    }
}