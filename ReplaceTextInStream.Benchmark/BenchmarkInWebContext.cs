using Alba;
using BenchmarkDotNet.Attributes;

namespace ReplaceTextInStream.Benchmark;

public class BenchmarkInWebContext
{
    //[Benchmark]
    //public async Task StreamReader()
    //{
    //    await _host.Scenario(s =>
    //    {
    //        s.Get.Url("/stream");
    //        s.StatusCodeShouldBeOk();
    //        s.ContentShouldNotContain("lorem");
    //    });
    //}

    [Benchmark(Baseline = true)]
    public async Task Pipes()
    {
        await _host.Scenario(s =>
        {
            s.Get.Url("/pipes");
            s.StatusCodeShouldBeOk();
        });
    }

    //[Benchmark]
    //public async Task Regex()
    //{
    //    await _host.Scenario(s =>
    //    {
    //        s.Get.Url("/regex");
    //        s.StatusCodeShouldBeOk();
    //        s.ContentShouldNotContain("lorem");
    //    });
    //}


    [Benchmark]
    public async Task String()
    {
        await _host.Scenario(s =>
        {
            s.Get.Url("/string");
            s.StatusCodeShouldBeOk();
        });
    }


    private IAlbaHost _host = null!;

    [GlobalSetup]
    public void Setup()
    {
        _host = AlbaHost.For<Web.Program>().GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _host.Dispose();
    }
}