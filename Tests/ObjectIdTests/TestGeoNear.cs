﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;

namespace MongoDB.Entities.Tests;

[TestClass]
public class GeoNearObjectIdTest
{
    [TestMethod]
    public async Task find_match_geo_method()
    {
        await DB.Index<PlaceObjectId>()
          .Key(x => x.Location, KeyType.Geo2DSphere)
          .Option(x => x.Background = false)
          .CreateAsync();

        var guid = Guid.NewGuid().ToString();

        await new[]
        {
            new PlaceObjectId { Name = "Paris "+ guid, Location = new(48.8539241, 2.2913515) },
            new PlaceObjectId { Name = "Versailles "+ guid, Location = new(48.796964, 2.137456) },
            new PlaceObjectId { Name = "Poissy "+ guid, Location = new(48.928860, 2.046889) }
        }.SaveAsync();

        var res = (await DB.Find<PlaceObjectId>()
                    .Match(p => p.Location, new(48.857908, 2.295243), 20000) //20km from eiffel tower
                    .Sort(p => p.ModifiedOn, Order.Descending)
                    .Limit(20)
                    .ExecuteAsync())
                    .Where(c => c.Name == "Paris " + guid)
                    .ToList();

        Assert.AreEqual(1, res.Count);
    }

    [TestMethod]
    public async Task geo_near_fluent_interface()
    {
        await DB.Index<PlaceObjectId>()
            .Key(x => x.Location, KeyType.Geo2DSphere)
            .Option(x => x.Background = false)
            .CreateAsync();

        var guid = Guid.NewGuid().ToString();

        await new[]
        {
            new PlaceObjectId { Name = "Paris "+ guid, Location = new(48.8539241, 2.2913515) },
            new PlaceObjectId { Name = "Versailles "+ guid, Location = new(48.796964, 2.137456) },
            new PlaceObjectId { Name = "Poissy "+ guid, Location = new(48.928860, 2.046889) }
        }.SaveAsync();

        var qry = DB.FluentGeoNear<PlaceObjectId>(
                     NearCoordinates: new(48.857908, 2.295243), //eiffel tower
                     DistanceField: x => x.DistanceKM,
                     MaxDistance: 20000);

        var cnt = await qry.Match(c => c.Name.Contains(guid)).ToListAsync();
        Assert.AreEqual(2, cnt.Count);

        var res = await qry.Match(c => c.Name == "Paris " + guid).ToListAsync();
        Assert.AreEqual(1, res.Count);
    }

    [TestMethod]
    public async Task geo_near_transaction_returns_correct_results()
    {
        await DB.Index<PlaceObjectId>()
            .Key(x => x.Location, KeyType.Geo2DSphere)
            .Option(x => x.Background = false)
            .CreateAsync();

        var guid = Guid.NewGuid().ToString();

        using var TN = new Transaction();

        await new[]
        {
            new PlaceObjectId { Name = "Paris "+ guid, Location = new(48.8539241, 2.2913515) },
            new PlaceObjectId { Name = "Versailles "+ guid, Location = new(48.796964, 2.137456) },
            new PlaceObjectId { Name = "Poissy "+ guid, Location = new(48.928860, 2.046889) }
        }.SaveAsync();

        var qry = TN.GeoNear<PlaceObjectId>(
                     NearCoordinates: new(48.857908, 2.295243), //eiffel tower
                     DistanceField: x => x.DistanceKM,
                     MaxDistance: 20000);

        var cnt = await qry.Match(c => c.Name.Contains(guid)).ToListAsync();
        Assert.AreEqual(2, cnt.Count);

        var res = await qry.Match(c => c.Name == "Paris " + guid).ToListAsync();
        Assert.AreEqual(1, res.Count);
    }
}