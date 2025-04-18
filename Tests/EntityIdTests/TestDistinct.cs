﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests;

[TestClass]
public class DistinctEntity
{
    [TestMethod]
    public async Task distinct_works()
    {
        var guid1 = Guid.NewGuid().ToString();
        var guid2 = Guid.NewGuid().ToString();
        var guids = new[] { guid1, guid2 };

        await new[] {
            new AuthorEntity{ Name = guid1 },
            new AuthorEntity{ Name = guid1 },
            new AuthorEntity{ Name = guid2 },
            new AuthorEntity{ Name = guid2 },
        }.SaveAsync();

        var res = await DB.Distinct<AuthorEntity, string>()
            .Match(a => guids.Contains(a.Name))
            .Property(a => a.Name)
            .ExecuteAsync();

        Assert.AreEqual(2, res.Count);
        Assert.IsTrue(!res.Except(guids).Any());
    }
}
