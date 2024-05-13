using System.Buffers;
using System.Globalization;
using System.IO.Pipelines;
using System.Text;

namespace ReplaceTextInStream.Test;

[TestFixture]
public class Experiments
{
    [Test]
    public void AdvancePastEndOfReader_ThrowsException()
    {
        var sequence = new ReadOnlySequence<byte>([1, 2, 3]);
        
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var reader = new SequenceReader<byte>(sequence);
            reader.Advance(4);
        });
    }

    [TestCase('a', new[] { 97 })]
    [TestCase('é', new[] { 195, 169 })]
    [TestCase('€', new[] { 226, 130, 172 })]
    public void CharToBytes(char c, int[] expectedBytes)
    {
        var bytes = Encoding.UTF8.GetBytes(new []{c});
        Assert.That(bytes, Is.EqualTo(expectedBytes));
    }

    [Test]
    public void PartialEncoding()
    {
        var array = "é€"u8.ToArray();
        var sequence = new ReadOnlySequence<byte>(array);

        var writer = new ArrayBufferWriter<char>();
        var decoder = Encoding.UTF8.GetDecoder();

        var firstSlice = sequence.Slice(0, 4);
        decoder.Convert(firstSlice, writer, false, out var charsUsed, out var completed);
        writer.Clear();
        decoder.Convert(sequence.Slice(4), writer, false, out var charsUsed2, out var completed2);

        Assert.That(writer, Is.Not.Null);
    }
}