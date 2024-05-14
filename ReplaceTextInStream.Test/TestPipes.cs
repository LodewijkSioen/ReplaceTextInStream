using System.Text;

namespace ReplaceTextInStream.Test;

[TestFixture]
public class TestPipes : BaseTests
{
    protected override IStreamingReplacer GetReplacer() => new UsingPipes();

    [TestCase("", "", TestName = "Empty")]
    [TestCase("abc", "123", TestName = "Just the text")]
    [TestCase("***ababc***", "***ab123***", TestName = "Almost match followed by match")]
    [TestCase("ABCabcAbcAbC", "123123123123", TestName = "Case Sensitivity")]
    [TestCase("abc€", "123€", TestName = "Multibyte characters")]
    public async Task TestStreamReaderCases(string inputText, string expectedResult)
    {
        var replacer = GetReplacer();
        await using var input = CreateInputStream(inputText);
        await using var output = new MemoryStream();

        await replacer.Replace(input, output, "abc", "123");

        await AssertOutputStream(output, expectedResult);
    }

    [Test]
    public async Task NonDefaultEncoding()
    {
        var replacer = new UsingPipes(Encoding.Latin1);

        await using var input = CreateInputStream("this is a test with another encoding", Encoding.Latin1);
        await using var output = new MemoryStream();

        await replacer.Replace(input, output, "test", "whatever");

        await AssertOutputStream(output, "this is a whatever with another encoding", Encoding.Latin1);
    }

    [Test]
    public async Task MismatchingEncodings()
    {
        var replacer = new UsingPipes(Encoding.UTF32);

        await using var input = CreateInputStream("this is a test with another encoding", Encoding.Latin1);
        await using var output = new MemoryStream();

        await replacer.Replace(input, output, "test", "whatever");

        await AssertOutputStream(output, "this is a test with another encoding", Encoding.Latin1);
    }
}