﻿using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Benchmark;

public static class Program
{
    static void Main()
    {
        BenchmarkRunner.Run(typeof(Program).Assembly,new DebugInProcessConfig());
    }
}

//dotnet run -p Benchmark.csproj -c Release
