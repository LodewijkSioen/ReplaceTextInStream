using System.Text;

namespace ReplaceTextInStream.Test;

public abstract class BaseTests
{
    private const string Input = @"Hello World, how are you doing? This world is Fine.
Worlds can be very nice, you know.";
    private const string Expected = @"Hello Galaxy, how are you doing? This Galaxy is Fine.
Galaxys can be very nice, you know.";

    protected abstract IStreamingReplacer GetReplacer();

    [Test]
    public async Task BaseLineTest()
    {
        var replacer = GetReplacer();
        await using var input = CreateInputStream(Input);
        await using var output = new MemoryStream();

        await replacer.Replace(input, output, "World", "Galaxy");

        await AssertOutputStream(output, Expected);
    }

    

    [Test]
    public async Task TestShorterNewValue()
    {
        var replacer = GetReplacer();
        await using var input = CreateInputStream("***abc***");
        await using var output = new MemoryStream();

        await replacer.Replace(input, output, "abc", "1");

        await AssertOutputStream(output, "***1***");
    }
    
    [Test]
    public async Task TestLongerNewValue()
    {
        var replacer = GetReplacer();
        await using var input = CreateInputStream("***abc***");
        await using var output = new MemoryStream();

        await replacer.Replace(input, output, "abc", "1234");

        await AssertOutputStream(output, "***1234***");
    }
    
    protected Stream CreateInputStream(string input)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(input));
    }

    protected async Task AssertOutputStream(Stream output, string expected)
    {
        output.Position = 0;
        var reader = new StreamReader(output);
        var result = await reader.ReadToEndAsync();
        Assert.That(result, Is.EqualTo(expected));
    }
}