﻿using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MongoDB.Driver.GridFS;
using MongoDB.Entities;

namespace Benchmark;

[MemoryDiagnoser]
public class FileStorageWrite : BenchBase
{
    static readonly MemoryStream memStream = new(new byte[32 * 1024 * 1024]);

    [Benchmark]
    public override async Task MongoDB_Entities()
    {
        memStream.Position = 0;

        var file = new File { Name = "file name here" };
        await file.SaveAsync();
        await file.Data.UploadAsync(memStream, 1024 * 4);
    }

    [Benchmark(Baseline = true)]
    public override async Task Official_Driver()
    {
        memStream.Position = 0;

        var bucket = new GridFSBucket(Database, new()
        {
            BucketName = "benchmark",
            ChunkSizeBytes = 4 * 1024 * 1024
        });

        await bucket.UploadFromStreamAsync("file name here", memStream);
    }
}
