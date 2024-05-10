using System.Text;

namespace ReplaceTextInStream;

public class UsingStringReplace : IStreamingReplacer
{
    public async Task Replace(Stream input, Stream output, string oldValue, string newValue, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(input, leaveOpen: true);
        var original = await reader.ReadToEndAsync();
        var replaced = original.Replace(oldValue, newValue, StringComparison.InvariantCultureIgnoreCase);
        await output.WriteAsync(Encoding.UTF8.GetBytes(replaced), cancellationToken);
    }
}

public class UsingStringReplaceCaseInsensitive : IStreamingReplacer
{
    public async Task Replace(Stream input, Stream output, string oldValue, string newValue, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(input, leaveOpen: true);
        var original = await reader.ReadToEndAsync();
        var replaced = original.Replace(oldValue, newValue);
        await output.WriteAsync(Encoding.UTF8.GetBytes(replaced), cancellationToken);
    }
}