namespace ReplaceTextInStream;

public interface IStreamingReplacer
{
    //begin-snippet: ReplaceInterface
    Task Replace(Stream input, Stream output, string oldValue, string newValue, CancellationToken cancellationToken = default);
    //end-snippet
}