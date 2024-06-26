using Alba;
using ReplaceTextInStream.Web;

namespace ReplaceTextInStream.Test;

[TestFixture]
public class TestWeb
{
    [TestCase("/pipes")]
    [TestCase("/regex")]
    [TestCase("/stream")]
    [TestCase("/string")]
    public async Task Pipes(string url)
    {
        var response = await _host.Scenario(s =>
        {
            s.Get.Url(url);
            s.StatusCodeShouldBeOk();
        });

        Assert.That(await response.ReadAsTextAsync(), Does.Not.Contain("lorem").IgnoreCase);
    }

    private IAlbaHost _host = null!;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _host = await AlbaHost.For<Program>();
    }

    [OneTimeTearDown]
    public async Task Teardown()
    {
        await _host.DisposeAsync();
    }
}