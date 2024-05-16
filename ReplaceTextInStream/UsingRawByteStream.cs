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
        var pattern = new Strategy(Encoding.Default, oldValue);
        var inputBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(_bufferLength, pattern.MaxLength * 2));
        var newValueInBytes = Encoding.Default.GetBytes(newValue);

        try
        {
            var startIndex = 0;

            while (true)
            {
                var memory = inputBuffer.AsMemory(startIndex, inputBuffer.Length - startIndex);
                var charactersRead = await input.ReadAsync(memory, cancellationToken);
                
                var sequence = new ReadOnlySequence<byte>(inputBuffer[..(charactersRead + startIndex)]);
                var currentSequenceLength = sequence.Length;
                var endOfStream = inputBuffer.Length - startIndex > charactersRead;

                while (true)
                {
                    if (pattern.FindPattern(ref sequence, out var inspected, endOfStream))
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

                if (charactersRead == 0)
                {
                    await output.WriteAsync(inputBuffer[..startIndex], cancellationToken);
                    break;
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
}