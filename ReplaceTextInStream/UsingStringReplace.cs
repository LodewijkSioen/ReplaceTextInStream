namespace ReplaceTextInStream;

public class UsingStringReplace(StringComparison comparisonType = StringComparison.OrdinalIgnoreCase) : IStreamingReplacer
{
    public async Task Replace(Stream input, Stream output, string oldValue, string newValue, CancellationToken cancellationToken = default)
    {
        //begin-snippet: StringReplace
        using var reader = new StreamReader(input, leaveOpen: true);
        var original = await reader.ReadToEndAsync(cancellationToken);
        var replaced = original.Replace(oldValue, newValue, comparisonType);
        await using var writer = new StreamWriter(output, leaveOpen: true);
        await writer.WriteAsync(replaced);
        //end-snippet
    }
}