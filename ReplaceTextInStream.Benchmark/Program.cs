using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;

#if DEBUG

_ = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());

#else
using ReplaceTextInStream.Benchmark;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Filters;

_ = BenchmarkRunner.Run<BenchmarkStreamingReplace>(
    ManualConfig.Create(DefaultConfig.Instance)
        .AddFilter(new SimpleFilter(c => !c.Descriptor.Categories.Contains("ignore", StringComparer.InvariantCultureIgnoreCase)))
        .AddDiagnoser(MemoryDiagnoser.Default));

#endif