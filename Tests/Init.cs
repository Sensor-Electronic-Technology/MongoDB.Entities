using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[TestClass]
public static class InitTest {
    static MongoClientSettings ClientSettings { get; set; } = new MongoClientSettings() {
        Server = new MongoServerAddress("172.20.3.41", 27017),
    };
    static bool _useTestContainers=false;

    [AssemblyInitialize]
    public static async Task Init(TestContext _)
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        _useTestContainers = Environment.GetEnvironmentVariable("MONGODB_ENTITIES_TESTCONTAINERS") != null;

        if (_useTestContainers)
        {
            var testContainer = await TestDatabase.CreateDatabase();
            ClientSettings = MongoClientSettings.FromConnectionString(testContainer.GetConnectionString());
        }

        await InitTestDatabase("mongodb-entities-test");
    }

    public static async Task InitTestDatabase(string databaseName)
    {
        if (_useTestContainers)
            await DB.InitAsync(databaseName, ClientSettings);
        else
            await DB.InitAsync(databaseName,ClientSettings);
    }

    public static async Task InitTestDatabase(string host="172.20.3.41",params Assembly[] assemblies) {
        await DB.InitAsync("epi_system", "172.20.3.41",enableLogging:false,assemblies:assemblies);
    }
}