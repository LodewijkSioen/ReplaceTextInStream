using System.Buffers;
using System.Globalization;
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

    [TestCase('a', new[] { 97 }, TestName = "'a' encodes to 1 byte")]
    [TestCase('é', new[] { 195, 169 }, TestName = "'é' encodes to 2 bytes")]
    [TestCase('€', new[] { 226, 130, 172 }, TestName = "'€' encodes to 3 byte")]
    public void CharToBytes(char c, int[] expectedBytes)
    {
        var bytes = Encoding.UTF8.GetBytes(new []{c});
        Assert.That(bytes, Is.EqualTo(expectedBytes));
    }

    [Test]
    public void PartialDecoding()
    {
        var array = "é€"u8.ToArray();
        var sequence = new ReadOnlySequence<byte>(array);

        var writer = new ArrayBufferWriter<char>();
        var decoder = Encoding.UTF8.GetDecoder();
        
        //slice contains all the bytes for 'é' and one byte of '€'
        var firstSlice = sequence.Slice(0, 3);
        decoder.Convert(firstSlice, writer, false, out _, out _);
        var firstString = new string(writer.WrittenSpan);
        Assert.That(firstString, Is.EqualTo("é"));

        //slice contains the last two bytes of '€'
        var secondSlice = sequence.Slice(3, 2);
        decoder.Convert(secondSlice, writer, false, out _, out _);
        Assert.That(new string(writer.WrittenSpan), Is.EqualTo("é€"));

        //So the decoder remembers the bytes that are not used for the next run
    }

    [TestCase('ſ', 'S', 2, 1, TestName = "Some characters have an ascii uppercase variant")]
    [TestCase('ȿ', 'Ȿ', 2, 3, TestName = "Some characters have more bytes in uppercase")]
    [TestCase('ⱦ', 'Ⱦ', 3, 2, TestName = "Some characters have more bytes in lowercase")]
    public void NotAllCharsHaveTheSameNumberOfBytesInDifferentCasing(char lower, char upper, int lowerCount, int upperCount)
    {
        Assert.That(char.ToUpper(lower), Is.EqualTo(upper));
        Assert.That(char.ToUpperInvariant(lower), Is.EqualTo(upper));
        if(upperCount != 1) //No two way conversion for this character
        {
            Assert.That(char.ToLower(upper), Is.EqualTo(lower));
            Assert.That(char.ToLowerInvariant(upper), Is.EqualTo(lower));
        }
        Assert.That(Encoding.UTF8.GetByteCount([lower]), Is.EqualTo(lowerCount));
        Assert.That(Encoding.UTF8.GetByteCount([upper]), Is.EqualTo(upperCount));
    }

    [TestCase("ſ", "S", false)] //Wierd that this is false, but okay
    [TestCase("ȿ", "Ȿ", true)]
    [TestCase("ⱦ", "Ⱦ", true)]
    public void EqualityOfThoseWierdos(string lower, string upper, bool expected)
    {
        Assert.That(lower.Equals(upper, StringComparison.OrdinalIgnoreCase), Is.EqualTo(expected));
    }

    [Test]
    public void TheTurkishThing()
    {
        Assert.That(char.ToUpperInvariant('ı'), Is.EqualTo('ı'));
        Assert.That(char.ToLowerInvariant('İ'), Is.EqualTo('İ'));
        Assert.That(char.ToUpperInvariant('i'), Is.EqualTo('I'));
        Assert.That(char.ToLowerInvariant('I'), Is.EqualTo('i'));

        var english = CultureInfo.GetCultureInfo("en");
        Assert.That(char.ToUpper('ı', english), Is.EqualTo('I'));
        Assert.That(char.ToLower('İ', english), Is.EqualTo('i'));
        Assert.That(char.ToUpper('i'), Is.EqualTo('I'));
        Assert.That(char.ToLower('I'), Is.EqualTo('i'));

        var turkish = CultureInfo.GetCultureInfo("tr-TR");
        Assert.That(char.ToUpper('ı', turkish), Is.EqualTo('I'));
        Assert.That(char.ToUpper('i', turkish), Is.EqualTo('İ'));
        Assert.That(char.ToLower('İ', turkish), Is.EqualTo('i'));
        Assert.That(char.ToLower('I', turkish), Is.EqualTo('ı'));
    }

    
    [Test, Explicit]
    public void CheckingStuff()
    {
        var allUpperCase = AllDotnetCharacters().Where(char.IsUpper).ToList();
        var allLowerCase = AllDotnetCharacters().Where(char.IsLower).ToList();

        var instersected = allLowerCase
            //.Select(c => (char.ToLowerInvariant(c), char.ToUpperInvariant(c), Encoding.UTF8.GetByteCount([char.ToLowerInvariant(c)]), Encoding.UTF8.GetByteCount([char.ToUpperInvariant(c)])))
            .Select(c => (c, char.ToLowerInvariant(c)))
            .Where(c => c.Item1 != c.Item2);

        var instersectedCount = instersected.Count();
    }

    private IEnumerable<char> AllDotnetCharacters()
    {
        for (int i = char.MinValue; i < char.MaxValue; i++)
        {
            yield return (char) i;
        }
    }
}