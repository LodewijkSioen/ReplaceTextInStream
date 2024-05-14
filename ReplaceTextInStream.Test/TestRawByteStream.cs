namespace ReplaceTextInStream.Test;

[TestFixture]
public class TestRawByteStream : BaseTests
{
    //Smallest possible buffer size is 16
    protected override IStreamingReplacer GetReplacer() => new UsingRawByteStream(bufferLength: 16);

    [TestCase("", "", TestName = "Empty")]
    [TestCase("abc", "123", TestName = "Just the text")]
    [TestCase("abc**********abcabc**********abc", "123**********123123**********123", TestName = "Beginning and end of buffer")]
    [TestCase("**************abc*****", "**************123*****", TestName = "Crossing buffer boundaries")]
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
}