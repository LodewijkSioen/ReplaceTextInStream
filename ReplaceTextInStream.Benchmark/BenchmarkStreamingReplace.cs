﻿using BenchmarkDotNet.Attributes;

namespace ReplaceTextInStream.Benchmark;

public class BenchmarkStreamingReplace
{
    [Benchmark(Baseline = true)]
    public async Task StringReplace()
    {
        await using var input = OpenInputStream();
        await using var output = OpenOutputStream();
        await new UsingStringReplace().Replace(input, output, "lorem", "schmorem");
    }

    [Benchmark]
    public async Task RegexReplace()
    {
        await using var input = OpenInputStream();
        await using var output = OpenOutputStream();
        await new UsingRegexReplace().Replace(input, output, "lorem", "schmorem");
    }

    [Benchmark]
    public async Task StreamReader()
    {
        await using var input = OpenInputStream();
        await using var output = OpenOutputStream();
        await new UsingStreamReader().Replace(input, output, "lorem", "schmorem");
    }

    [Benchmark]
    public async Task RawByteStream()
    {
        await using var input = OpenInputStream();
        await using var output = OpenOutputStream();
        await new UsingRawByteStream().Replace(input, output, "lorem", "schmorem");
    }

    [Benchmark]
    public async Task Pipes()
    {
        await using var input = OpenInputStream();
        await using var output = OpenOutputStream();
        await new UsingPipes().Replace(input, output, "lorem", "schmorem");
    }

    [Benchmark, BenchmarkCategory("Ignore")]
    public async Task StringReplaceInvariant()
    {
        await using var input = OpenInputStream();
        await using var output = OpenOutputStream();
        await new UsingStringReplaceInvariant().Replace(input, output, "lorem", "schmorem");
    }
    
    private Stream OpenInputStream()
    {
        return File.OpenRead("LoremIpsum.txt");
    }

    private Stream OpenOutputStream()
    {
        return Stream.Null;
    }
}