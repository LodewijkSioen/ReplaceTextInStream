using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace ReplaceTextInStream;

public class UsingPipes(Encoding? encoding = null) 
    : IStreamingReplacer
{
    private readonly Encoding _encoding = encoding ?? Encoding.Default;

    public Task Replace(Stream input, Stream output, string oldValue, string newValue,
        CancellationToken cancellationToken = default)
    {
        var pipe = new Pipe();

        var reading = FillPipeAsync(input, pipe.Writer, cancellationToken);
        var writing = WriteToOutput(pipe.Reader, output, oldValue, newValue, cancellationToken);

        return Task.WhenAll(reading, writing);
    }

    async Task FillPipeAsync(Stream input, PipeWriter writer, CancellationToken cancellationToken)
    {
        while (true)
        {
            //Read some stuff from the input stream
            var memory = writer.GetMemory();
            
            var bytesRead = await input.ReadAsync(memory, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            // Tell the PipeWriter how much was read from the stream.
            writer.Advance(bytesRead);
            
            // Make the data available to the PipeReader.
            var result = await writer.FlushAsync(cancellationToken);

            if (result.IsCompleted)
            {
                break;
            }
        }

        // By completing PipeWriter, tell the PipeReader that there's no more data coming.
        await writer.CompleteAsync();
    }

    private async Task WriteToOutput(PipeReader reader, Stream output, string oldValue, string newValue,
        CancellationToken cancellationToken)
    {
        var pattern = new Pattern(_encoding, oldValue);
        var newValueInBytes = _encoding.GetBytes(newValue);
        
        while (true)
        {
            //Read some stuff from the pipe
            var result = await reader.ReadAsync(cancellationToken);
            var sequence = result.Buffer;

            while (true)
            {
                if (FindPattern(ref sequence, pattern, out var inspected))
                {
                    //If the pattern is found, write the inspected slice and the replacement newvalue
                    await output.WriteAsync(inspected.ToArray(), cancellationToken);
                    await output.WriteAsync(newValueInBytes, cancellationToken);
                }
                else
                {
                    //If the pattern is not found, just write the inspected part and exit
                    await output.WriteAsync(inspected.ToArray(), cancellationToken);
                    break;
                }
            }

            // Signal to the pipereader what part we have consumed
            reader.AdvanceTo(sequence.Start, sequence.End);

            if (result.IsCompleted)
            {
                // Write the remaining bytes to the output
                if (!sequence.IsEmpty)
                {
                    await output.WriteAsync(sequence.ToArray(), cancellationToken);
                }
                break;
            }
        }
        
        await reader.CompleteAsync();
    }

    /// <summary>
    /// Finds the first occurence of the Pattern in a sequence of bytes
    /// </summary>
    /// <param name="haystack">
    /// The sequence of bytes that might contain the pattern. Passed by reference and will be resliced to
    /// only contain the part of the sequence that has not been inspected. If the pattern is found, the
    /// haystack will be resliced at the end of the pattern.
    /// </param>
    /// <param name="pattern">The pattern to match in the haystack</param>
    /// <param name="inspected">
    /// The slice of the haystack that has been inpected:
    ///  - If the pattern is found, this will contain all the bytes up to the pattern
    ///  - If the the pattern is not found, this will contain all the bytes of the haystack
    ///  - If the first byte of the pattern is found but there are not enough bytes left in the haystack
    ///    to match the pattern, this will contain all the bytes up to the first byte of the pattern
    /// </param>
    /// <returns>True if the pattern is found in the haystack, otherwise false</returns>
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

                if (CompareSequence(ref reader, pattern))
                {
                    inspected = haystack.Slice(0, reader.Position);
                    reader.Advance(pattern.LengthInBytes);
                    haystack = haystack.Slice(reader.Position);
                    return true;
                }

                reader.Advance(pattern.LengthInBytes);
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
        for (var i = 1; i < pattern.LengthInBytes; i++)
        {
            if (!reader.TryPeek(i, out var v)
                || (v != pattern.UpperBytes[i] && v != pattern.LowerBytes[i]))
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