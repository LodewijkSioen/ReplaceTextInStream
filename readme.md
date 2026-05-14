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

| Method        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated   | Alloc Ratio |
|-------------- |---------:|----------:|----------:|------:|--------:|---------:|---------:|---------:|------------:|------------:|
| StringReplace | 6.003 ms | 0.2293 ms | 0.6690 ms |  1.01 |    0.16 | 492.1875 | 445.3125 |  93.7500 | 12469.23 KB |       1.000 |
| RegexReplace  | 8.545 ms | 0.1700 ms | 0.4538 ms |  1.44 |    0.18 | 812.5000 | 796.8750 | 421.8750 | 12484.42 KB |       1.001 |
| StreamReader  | 6.870 ms | 0.1370 ms | 0.3006 ms |  1.16 |    0.14 | 726.5625 |  46.8750 |        - |  8540.74 KB |       0.685 |
| RawByteStream | 3.701 ms | 0.0736 ms | 0.1125 ms |  0.62 |    0.07 | 171.8750 |        - |        - |  2111.73 KB |       0.169 |
| Pipes         | 3.396 ms | 0.0643 ms | 0.0740 ms |  0.57 |    0.07 |        - |        - |        - |     3.47 KB |       0.000 |