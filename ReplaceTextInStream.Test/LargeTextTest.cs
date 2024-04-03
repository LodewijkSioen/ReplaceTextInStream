namespace ReplaceTextInStream.Test;

[TestFixture]
public class LargeTextTest
{
    private Stream _input = null!;
    [SetUp]
    public void Setup()
    {
        _input = File.OpenRead("LoremIpsum.txt");
    }

    [TearDown]
    public void Teardown()
    {
        _input?.Dispose();
    }

    [Test]
    public async Task StreamReader()
    {
        var replacer = new UsingStreamReader();

        await using var output = new MemoryStream();

        await replacer.Replace(_input, output, "lorem", "12345");

        output.Position = 0;
        var reader = new StreamReader(output);
        var result = await reader.ReadToEndAsync();
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Not.Contain("Lorem").IgnoreCase);
    }

    [Test]
    public async Task Pipes()
    {
        var replacer = new UsingPipes();

        await using var output = new MemoryStream();

        await replacer.Replace(_input, output, "lorem", "12345");

        output.Position = 0;
        var reader = new StreamReader(output);
        var result = await reader.ReadToEndAsync();
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Not.Contain("lorem").IgnoreCase);
    }

    [Explicit, Test, Repeat(100)]
    public async Task ProfileLargeText()
    {
        var replacer = new UsingPipes();

        var output = Stream.Null;

        await replacer.Replace(_input, output, "lorem", "12345");
    }
}