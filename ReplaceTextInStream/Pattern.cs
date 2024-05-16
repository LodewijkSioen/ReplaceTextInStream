using System.Buffers;
using System.Text;

namespace ReplaceTextInStream;

public readonly record struct CharByteMap
{
    public CharByteMap(Encoding encoding, char c)
    {
        Upper = encoding.GetBytes(new[]{char.ToUpperInvariant(c)});
        Lower = encoding.GetBytes(new[]{char.ToLowerInvariant(c)});
    }

    public byte[] Upper { get; }
    public byte[] Lower { get; }

    public bool IsNext(ref SequenceReader<byte> reader, bool advancePast)
    {
        return reader.IsNext(Upper, advancePast) || reader.IsNext(Lower, advancePast);
    }
}

public readonly record struct Pattern
{
    public Pattern(Encoding encoding, string value)
    {
        Bytes = value.Select(c => new CharByteMap(encoding, c)).ToArray();
        Delimiters = [Bytes[0].Lower[0], Bytes[0].Upper[0]];
        MaxLength = Bytes.Aggregate(0, (acc, cur) => acc + Math.Max(cur.Lower.Length, cur.Upper.Length));
    }

    private CharByteMap[] Bytes { get; }
    private byte[] Delimiters { get; }
    public int MaxLength { get; }

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
    /// <param name="isFinalRun">Indicates that there are no more bytes to get after this sequence</param>
    /// <returns>True if the pattern is found in the haystack, otherwise false</returns>
    public bool FindPattern(ref ReadOnlySequence<byte> haystack, out ReadOnlySequence<byte> inspected, bool isFinalRun)
    {
        var reader = new SequenceReader<byte>(haystack);

        while (true)
        {
            if (TryAdvanceToPossibleSequence(ref reader))
            {
                if (!isFinalRun && reader.Remaining < MaxLength)
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
    /// Advances the reader to the first byte of this pattern.
    /// </summary>
    /// <param name="reader">
    /// The sequence of bytes containing the bytes to find the pattern.
    /// If the pattern is found, the reader will be at the first byte.
    /// </param>
    /// <returns>True if the byte sequence of the first character is found, otherwise false</returns>
    private bool TryAdvanceToPossibleSequence(ref SequenceReader<byte> reader)
    {
        //Find the first byte of this pattern
        while (reader.TryAdvanceToAny(Delimiters, false))
        {
            //Is this first byte part of the bytes that make up the first character?
            if (Bytes[0].IsNext(ref reader, false))
            {
                return true;
            }

            //If not, skip this byte and search further
            reader.Advance(1);
        }
        return false;
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
        foreach (var b in Bytes)
        {
            if (!b.IsNext(ref reader, true))
            {
                return false;
            }
        }

        return true;
    }
}