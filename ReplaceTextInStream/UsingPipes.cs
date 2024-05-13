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
                //Find the pattern in the sequence. The sequence is byref and will
                //be resliced
                if (FindPattern(ref sequence, pattern, out var before))
                {
                    await output.WriteAsync(before.ToArray(), cancellationToken);
                    await output.WriteAsync(newValueInBytes, cancellationToken);
                }
                else
                {
                    await output.WriteAsync(before.ToArray(), cancellationToken);
                    await output.WriteAsync(sequence.ToArray(), cancellationToken);
                    break;
                }
            }

            reader.AdvanceTo(sequence.Start, sequence.End);

            if (result.IsCompleted)
            {
                break;
            }
        }

        
        await reader.CompleteAsync();
    }

    private bool FindPattern(ref ReadOnlySequence<byte> haystack, Pattern pattern, out ReadOnlySequence<byte> before)
    {
        var reader = new SequenceReader<byte>(haystack);

        while (true)
        {
            if (reader.TryAdvanceToAny(pattern.Delimiters, false))
            {
                if (reader.Remaining < pattern.LengthInBytes)
                {
                    before = haystack.Slice(0, reader.Position);
                    haystack = haystack.Slice(reader.Position);
                    return false;
                }

                if (CompareSequence(ref reader, pattern))
                {
                    before = haystack.Slice(0, reader.Position);
                    reader.Advance(pattern.LengthInBytes);
                    haystack = haystack.Slice(reader.Position);
                    return true;
                }

                reader.Advance(pattern.LengthInBytes);
            }
            else
            {
                reader.AdvanceToEnd();
                before = default;
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