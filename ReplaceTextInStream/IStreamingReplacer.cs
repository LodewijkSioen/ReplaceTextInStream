namespace ReplaceTextInStream;

public interface IStreamingReplacer
{
    Task Replace(Stream input, Stream output, string oldValue, string newValue, CancellationToken cancellationToken = default);
}