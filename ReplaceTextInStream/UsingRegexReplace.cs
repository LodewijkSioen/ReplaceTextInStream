using System.Text;
using System.Text.RegularExpressions;

namespace ReplaceTextInStream;

public class UsingRegexReplace : IStreamingReplacer
{
    public async Task Replace(Stream input, Stream output, string oldValue, string newValue, CancellationToken cancellationToken = default)
    {
        var regex = new Regex(oldValue,
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        using var reader = new StreamReader(input, leaveOpen: true);
        var original = await reader.ReadToEndAsync();
        var replaced = regex.Replace(original, newValue);
        await output.WriteAsync(Encoding.UTF8.GetBytes(replaced));
    }
}