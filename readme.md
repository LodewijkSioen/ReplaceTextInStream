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
| StringReplace | 7.775 ms | 0.1451 ms | 0.3305 ms |  1.00 |    0.06 | 445.3125 | 406.2500 |  78.1250 | 12468.99 KB |       1.000 |
| RegexReplace  | 7.245 ms | 0.1421 ms | 0.3266 ms |  0.93 |    0.06 | 796.8750 | 718.7500 | 421.8750 | 12484.09 KB |       1.001 |
| StreamReader  | 6.407 ms | 0.0660 ms | 0.0617 ms |  0.83 |    0.03 | 726.5625 |  46.8750 |        - |  8540.74 KB |       0.685 |
| RawByteStream | 3.691 ms | 0.0560 ms | 0.0468 ms |  0.48 |    0.02 | 343.7500 |        - |        - |  4273.79 KB |       0.343 |
| Pipes         | 3.383 ms | 0.0426 ms | 0.0377 ms |  0.44 |    0.02 |        - |        - |        - |     3.43 KB |       0.000 |