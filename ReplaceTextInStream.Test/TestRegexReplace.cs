namespace ReplaceTextInStream.Test;

[TestFixture]
public class TestRegexReplace : BaseTests
{
    protected override IStreamingReplacer GetReplacer() => new UsingRegexReplace();
}