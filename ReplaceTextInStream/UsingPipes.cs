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
                if (pattern.FindPattern(ref sequence, out var inspected, result.IsCompleted))
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
}