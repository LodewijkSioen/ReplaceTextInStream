# Replace A Text In A Stream

Code for a series of blogposts on [my blog](http://www.correlatedcontent.com)

Uses [mdsnippets](https://github.com/SimonCropp/MarkdownSnippets) to create
the code blocks in the posts.

## Benchmark

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.8246/24H2/2024Update/HudsonValley)
13th Gen Intel Core i7-13700H 2.40GHz, 1 CPU, 20 logical and 14 physical cores
.NET SDK 10.0.300
  [Host]     : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3

 Method        | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|-------------- |---------:|----------:|----------:|---------:|------:|--------:|---------:|---------:|---------:|----------:|------------:|
| StringReplace | 5.917 ms | 0.1826 ms | 0.5210 ms | 5.932 ms |  1.01 |    0.12 | 476.5625 | 453.1250 |  85.9375 |  12.18 MB |        1.00 |
| RegexReplace  | 8.823 ms | 0.1752 ms | 0.3772 ms | 8.828 ms |  1.50 |    0.15 | 812.5000 | 796.8750 | 421.8750 |  12.23 MB |        1.00 |
| StreamReader  | 7.382 ms | 0.3594 ms | 1.0370 ms | 7.229 ms |  1.26 |    0.21 | 718.7500 |  46.8750 |        - |   8.34 MB |        0.68 |
| RawByteStream | 4.095 ms | 0.0793 ms | 0.2062 ms | 4.086 ms |  0.70 |    0.07 | 343.7500 |        - |        - |   4.17 MB |        0.34 |
| Pipes         | 2.369 ms | 0.0696 ms | 0.2021 ms | 2.307 ms |  0.40 |    0.05 | 171.8750 |        - |        - |   2.07 MB |        0.17 |
