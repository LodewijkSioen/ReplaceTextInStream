namespace ReplaceTextInStream.Test;

[TestFixture]
public class TestStringReplace : BaseTests
{
    protected override IStreamingReplacer GetReplacer() => new UsingStringReplace();
}