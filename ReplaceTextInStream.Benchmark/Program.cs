using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;

#if DEBUG

_ = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());

#else
using ReplaceTextInStream.Benchmark;
using BenchmarkDotNet.Diagnosers;

_ = BenchmarkRunner.Run<BenchmarkStreamingReplace>(
    ManualConfig.Create(DefaultConfig.Instance)
        .AddDiagnoser(MemoryDiagnoser.Default));

#endif