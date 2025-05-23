﻿using System;
using MongoDB.Entities;

namespace Benchmark;

public class Book : Entity
{
    public string Title { get; set; } = null!;
    public One<Author> Author { get; set; } = null!;
    public DateTime PublishedOn { get; set; }
}
