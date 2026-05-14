using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace ReplaceTextInStream;

public class UsingPipes(Encoding? encoding = null) 
    : IStreamingReplacer
{
    private readonly Encoding _encoding = encoding ?? Encoding.Default;

    public async Task Replace(Stream input, Stream output, string oldValue, string newValue,
        CancellationToken cancellationToken = default)
    {
        var pattern = new Strategy(_encoding, oldValue);
        var newValueInBytes = _encoding.GetBytes(newValue);

        var reader = PipeReader.Create(input, new(leaveOpen: true));
        var writer = PipeWriter.Create(output, new(leaveOpen: true));

        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken);
                var sequence = result.Buffer;

                while (true)
                {
                    if (pattern.FindPattern(ref sequence, out var inspected, result.IsCompleted))
                    {
                        // Pattern found: write everything before the match, then the replacement
                        foreach (var segment in inspected)
                            writer.Write(segment.Span);
                        writer.Write(newValueInBytes);
                    }
                    else
                    {
                        // No more matches: write the remaining bytes and stop
                        foreach (var segment in inspected)
                            writer.Write(segment.Span);
                        break;
                    }
                }

                if (result.IsCompleted)
                {
                    // Safety: write any bytes not consumed by the inner loop, then advance past them
                    foreach (var segment in sequence)
                        writer.Write(segment.Span);
                    reader.AdvanceTo(sequence.End, sequence.End);
                    break;
                }

                reader.AdvanceTo(sequence.Start, sequence.End);
                await writer.FlushAsync(cancellationToken);
            }

            await writer.FlushAsync(cancellationToken);
        }
        finally
        {
            await reader.CompleteAsync();
            await writer.CompleteAsync();
        }
    }
}