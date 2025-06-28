using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;

namespace MongoDB.Entities;

static class TypeMap {
    static readonly ConcurrentDictionary<Type, IMongoDatabase> _typeToDbMap = new();
    static readonly ConcurrentDictionary<Type, string> _typeToCollMap = new();
    static readonly ConcurrentDictionary<Type, TypeConfiguration?> _typeToCollectionMap = new();
    static readonly ConcurrentDictionary<Type, EmbeddedTypeConfiguration?> _typeToEmbeddedCollectMap = new();

    internal static void AddCollectionMapping(Type entityType, string collectionName)
        => _typeToCollMap[entityType] = collectionName;

    internal static string? GetCollectionName(Type entityType) {
        _typeToCollMap.TryGetValue(entityType, out var name);

        return name;
    }

    internal static void AddDatabaseMapping(Type entityType, IMongoDatabase database)
        => _typeToDbMap[entityType] = database;

    internal static void Clear() {
        _typeToDbMap.Clear();
        _typeToCollMap.Clear();
        _typeToCollectionMap.Clear();
        _typeToEmbeddedCollectMap.Clear();
    }

    internal static TypeConfiguration? GetTypeConfiguration(Type entityType) {
        _typeToCollectionMap.TryGetValue(entityType, out var configuration);

        return configuration;
    }

    internal static EmbeddedTypeConfiguration? GetEmbeddedTypeConfiguration(Type entityType) {
        _typeToEmbeddedCollectMap.TryGetValue(entityType, out var configuration);

        return configuration;
    }

    internal static ICollection<Type> GetEmbeddedTypeConfigurationKeys()
        => _typeToEmbeddedCollectMap.Keys;

    internal static ICollection<Type> GetTypeConfigurationKeys()
        => _typeToCollectionMap.Keys;

    internal static void AddUpdateTypeConfiguration(Type entityType, TypeConfiguration? typeConfiguration)
        => _typeToCollectionMap[entityType] = typeConfiguration;

    internal static void AddUpdateEmbeddedTypeConfiguration(Type entityType,
                                                            EmbeddedTypeConfiguration? typeConfiguration)
        => _typeToEmbeddedCollectMap[entityType] = typeConfiguration;

    internal static void ClearTypeConfigurations()
        => _typeToCollectionMap.Clear();

    internal static void ClearEmbeddedTypeConfigurations()
        => _typeToEmbeddedCollectMap.Clear();

    internal static IMongoDatabase GetDatabase(Type entityType) {
        _typeToDbMap.TryGetValue(entityType, out var db);

        return db ?? DB.Database(default);
    }
}