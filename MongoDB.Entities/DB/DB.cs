using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
/// <summary>
/// The main entrypoint for all data access methods of the library
/// </summary>
public static partial class DB {
    static DB() {
        BsonSerializer.RegisterSerializer(new DateSerializer());
        BsonSerializer.RegisterSerializer(new FuzzyStringSerializer());
        BsonSerializer.RegisterSerializer(new DocumentVersionSerializer());
        BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
        BsonSerializer.RegisterSerializer(
            typeof(decimal?),
            new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));

        ConventionRegistry.Register(
            "DefaultConventions",
            new ConventionPack {
                new IgnoreExtraElementsConvention(true),
                new IgnoreManyPropsConvention()
            },
            _ => true);
    }

    internal static event Action? DefaultDbChanged;
    static readonly ConcurrentDictionary<string, IMongoDatabase> _dbs = new();
    static IMongoDatabase? _defaultDb;
    private static readonly ILogger _logger = AppLogger.CreateLogger("DB");
    public static bool LoggingEnabled { get; set; }

    /// <summary>
    /// Initializes a MongoDB connection with the given connection parameters.
    /// <para>WARNING: will throw an error if server is not reachable!</para>
    /// You can call this method as many times as you want (such as in serverless functions) with the same parameters and the connections won't get
    /// duplicated.
    /// </summary>
    /// <param name="database">Name of the database</param>
    /// <param name="host">Address of the MongoDB server</param>
    /// <param name="port">Port number of the server</param>
    /// <param name="enableLogging">Enable internal logging</param>
    /// <param name="assemblies">assemblies to scan for types of DocumentEntity</param>
    public static Task InitAsync(string database,
                                 string host = "127.0.0.1",
                                 int port = 27017,
                                 bool enableLogging = false,
                                 params Assembly[] assemblies) {
        LoggingEnabled = enableLogging;

        return InitializeWithTypeConfig(new() { Server = new(host, port) }, database, assemblies: assemblies);
    }

    /// <summary>
    /// Initializes a MongoDB connection with the given connection parameters.
    /// <para>WARNING: will throw an error if server is not reachable!</para>
    /// You can call this method as many times as you want (such as in serverless functions) with the same parameters and the connections won't get
    /// duplicated.
    /// </summary>
    /// <param name="database">Name of the database</param>
    /// <param name="settings">A MongoClientSettings object</param>
    public static Task InitAsync(string database, MongoClientSettings settings)
        => Initialize(settings, database);

    /// <summary>
    /// Initializes a MongoDB connection, scans given assemblies for DocumentEntity types,
    /// and preloads TypeConfiguration for the types scanned from the assemblies
    /// <para>WARNING: will throw an error if server is not reachable!</para>
    /// You can call this method as many times as you want (such as in serverless functions) with the same parameters and the connections won't get
    /// duplicated.
    /// </summary>
    /// <param name="database">Name of the database</param>
    /// <param name="host">Address of the MongoDB server</param>
    /// <param name="port">Port number of the server</param>
    public static Task InitAsync(string database, string host = "127.0.0.1", int port = 27017)
        => Initialize(new() { Server = new(host, port) }, database);

    internal static async Task InitializeWithTypeConfig(MongoClientSettings settings,
                                                        string dbName,
                                                        bool skipNetworkPing = false,
                                                        params Assembly[] assemblies) {
        Log(LogLevel.Information, "Initializing database...");

        if (string.IsNullOrEmpty(dbName)) {
            var exception = new ArgumentNullException(nameof(dbName), "Database name cannot be empty!");
            Log(logLevel: LogLevel.Critical, "Database name empty", exception);

            throw exception;
        }

        if (_dbs.ContainsKey(dbName)) {
            return;
        }

        try {
            var db = new MongoClient(settings).GetDatabase(dbName);

            if (_dbs.Count == 0) {
                _defaultDb = db;
            }

            if (_dbs.TryAdd(dbName, db) && !skipNetworkPing) {
                await db.RunCommandAsync((Command<BsonDocument>)"{ping:1}").ConfigureAwait(false);
            }
            await ScanAssemblies(assemblies);
            InitTypeConfigWatcher();
        } catch (Exception e) {
            Log(LogLevel.Critical, "Failed to initialize database", e);
            _dbs.TryRemove(dbName, out _);

            throw;
        }
    }

    internal static async Task ScanAssemblies(params Assembly[] assemblies) {
        Log(LogLevel.Information, "Scanning assemblies for DocumentEntities");
        var typeConfigCollection = Cache<TypeConfiguration>.Collection;

        foreach (var assembly in assemblies) {
            var types = assembly.DefinedTypes.Where(e => e.BaseType == typeof(DocumentEntity)).ToList();

            foreach (var type in types) {
                var collectionAttribute = type.GetCustomAttribute<CollectionAttribute>(false);
                var collectionName = collectionAttribute != null ? collectionAttribute.Name : type.Name;
                var typeConfig = await typeConfigCollection.Find(e => e.CollectionName == collectionName)
                                                           .SingleOrDefaultAsync();
                TypeMap.AddUpdateTypeConfiguration(type, typeConfig);
            }
        }
    }

    internal static async Task Initialize(MongoClientSettings settings, string dbName, bool skipNetworkPing = false) {
        if (string.IsNullOrEmpty(dbName))
            throw new ArgumentNullException(nameof(dbName), "Database name cannot be empty!");

        if (_dbs.ContainsKey(dbName))
            return;

        try {
            var db = new MongoClient(settings).GetDatabase(dbName);
            if (_dbs.IsEmpty)
                _defaultDb = db;

            if (_dbs.TryAdd(dbName, db) && !skipNetworkPing)
                await db.RunCommandAsync((Command<BsonDocument>)"{ping:1}").ConfigureAwait(false);
        } catch (Exception e) {
            _dbs.TryRemove(dbName, out _);
            Log(LogLevel.Critical, "Failed to initialize database {Database}", e, dbName);

            throw;
        }
    }

    internal static void InitTypeConfigWatcher() {
        Log(LogLevel.Information, "Creating watcher for TypConfigurations");
        var watcher = Watcher<TypeConfiguration>("type_config_internal_watcher");
        watcher.Start(
            eventTypes: EventType.Created | EventType.Updated | EventType.Deleted,
            batchSize: 5,
            onlyGetIDs: false,
            autoResume: true,
            cancellation: CancellationToken.None);
        watcher.OnChangesCSD += HandleTypeConfigChanges;
        watcher.OnError += (exception) => {
            Log(LogLevel.Critical, "Watcher error", exception);
        };
        watcher.OnStop += () => {
            Log(LogLevel.Warning, "Stopping watcher for TypConfigurations");

            if (watcher.CanRestart) {
                watcher.ReStart();
                Log(LogLevel.Information, "TypeConfigurations watcher restarted");
            } else {
                Log(LogLevel.Warning, "TypeConfigurations watcher failed to restart");
            }
        };
    }

    internal static void HandleTypeConfigChanges(IEnumerable<ChangeStreamDocument<TypeConfiguration>> changes) {
        foreach (var change in changes) {
            switch (change.OperationType) {
                case ChangeStreamOperationType.Delete: {
                    var typeConfig = change.FullDocumentBeforeChange;
                    var type = Type.GetType(typeConfig.TypeName);
                    if (type != null) {
                        AddUpdateTypeConfiguration(type, null);
                        Log(LogLevel.Information, "Type {TypeName} is deleted from TypeConfigurationMap", args: type);
                    }
                    break;
                }

                case ChangeStreamOperationType.Insert:
                case ChangeStreamOperationType.Update:
                case ChangeStreamOperationType.Replace: {
                    var typeConfig = change.FullDocument;
                    var type = Type.GetType(typeConfig.TypeName);

                    if (type != null) {
                        AddUpdateTypeConfiguration(type, typeConfig);
                        Log(
                            LogLevel.Information,
                            "Type {TypeName} Operation {Operation} completed",
                            type,
                            change.OperationType);
                    }

                    break;
                }
            }
        }
    }

    /// <summary>
    /// Gets a list of all database names from the server
    /// </summary>
    /// <param name="host">Address of the MongoDB server</param>
    /// <param name="port">Port number of the server</param>
    public static Task<IEnumerable<string>> AllDatabaseNamesAsync(string host = "127.0.0.1", int port = 27017)
        => AllDatabaseNamesAsync(new() { Server = new(host, port) });

    /// <summary>
    /// Gets a list of all database names from the server
    /// </summary>
    /// <param name="settings">A MongoClientSettings object</param>
    public static async Task<IEnumerable<string>> AllDatabaseNamesAsync(MongoClientSettings settings)
        => await (await new MongoClient(settings).ListDatabaseNamesAsync().ConfigureAwait(false)).ToListAsync()
               .ConfigureAwait(false);

    /// <summary>
    /// Specifies the database that a given entity type should be stored in.
    /// Only needed for entity types you want stored in a db other than the default db.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="database">The name of the database</param>
    public static void DatabaseFor<T>(string database) where T : IEntity
        => TypeMap.AddDatabaseMapping(typeof(T), Database(database));

    /// <summary>
    /// Gets the IMongoDatabase for the given entity type
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    public static IMongoDatabase Database<T>() where T : IEntity
        => Cache<T>.Database;

    /// <summary>
    /// Gets the IMongoDatabase for a given database name if it has been previously initialized.
    /// You can also get the default database by passing 'default' or 'null' for the name parameter.
    /// </summary>
    /// <param name="name">The name of the database to retrieve</param>
    public static IMongoDatabase Database(string? name) {
        IMongoDatabase? db = null;

        if (_dbs.Count == 0) {
            return db ??
                   throw new InvalidOperationException(
                       $"Database connection is not initialized for [{(string.IsNullOrEmpty(name) ? "Default" : name)}]");
        }
        if (string.IsNullOrEmpty(name))
            db = _defaultDb;
        else
            _dbs.TryGetValue(name, out db);

        return db ??
               throw new InvalidOperationException(
                   $"Database connection is not initialized for [{(string.IsNullOrEmpty(name) ? "Default" : name)}]");
    }

    /// <summary>
    /// Gets the name of the database a given entity type is attached to. Returns name of default database if not specifically attached.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static string DatabaseName<T>() where T : IEntity
        => Cache<T>.DbName;

    /// <summary>
    /// Gets the TypeConfiguration for the give type of DocumentEntity
    /// </summary>
    /// <typeparam name="TEntity">Any class that implements DocumentEntity</typeparam>
    public static TypeConfiguration? TypeConfiguration<TEntity>() where TEntity : DocumentEntity
        => TypeMap.GetTypeConfiguration(typeof(TEntity));

    /// <summary>
    /// Gets the TypeConfiguration for the given Type of DocumentEntity
    /// </summary>
    /// <param name="type">Type of type DocumentEntity to get the TypeConfiguration for</param>
    public static TypeConfiguration? TypeConfiguration(Type type)
        => TypeMap.GetTypeConfiguration(type);

    /// <summary>
    /// Updates the TypeConfiguration for the give type of DocumentEntity
    /// </summary>
    /// <param name="type">Type of type DocumentEntity</param>
    /// <param name="typeConfig">TypeConfiguration of the given type</param>
    public static void AddUpdateTypeConfiguration(Type type, TypeConfiguration? typeConfig)
        => TypeMap.AddUpdateTypeConfiguration(type, typeConfig);

    /// <summary>
    /// Switches the default database at runtime
    /// <para>WARNING: Use at your own risk!!! Might result in entities getting saved in the wrong databases under high concurrency situations.</para>
    /// <para>TIP: Make sure to cancel any watchers (change-streams) before switching the default database.</para>
    /// </summary>
    /// <param name="name">The name of the database to mark as the new default database</param>
    public static void ChangeDefaultDatabase(string name) {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name), "Database name cannot be null or empty");

        _defaultDb = Database(name);
        TypeMap.Clear();
        DefaultDbChanged?.Invoke();
    }

    /// <summary>
    /// Exposes the mongodb Filter Definition Builder for a given type.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static FilterDefinitionBuilder<T> Filter<T>() where T : IEntity
        => Builders<T>.Filter;

    /// <summary>
    /// Exposes the mongodb Sort Definition Builder for a given type.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static SortDefinitionBuilder<T> Sort<T>() where T : IEntity
        => Builders<T>.Sort;

    /// <summary>
    /// Exposes the mongodb Projection Definition Builder for a given type.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static ProjectionDefinitionBuilder<T> Projection<T>() where T : IEntity
        => Builders<T>.Projection;

    /// <summary>
    /// Returns a new instance of the supplied IEntity type
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static T Entity<T>() where T : IEntity, new()
        => new();

    /// <summary>
    /// Returns a new instance of the supplied IEntity type with the ID set to the supplied value
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="ID">The ID to set on the returned instance</param>
    public static T Entity<T>(object ID) where T : IEntity, new() {
        var newT = new T();
        newT.SetId(ID);

        return newT;
    }

    /// <summary>
    /// Internal logger for DB
    /// </summary>
    /// <param name="logLevel"></param>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    /// <param name="args"></param>
    private static void Log(LogLevel logLevel,
                            [StructuredMessageTemplate] string message,
                            Exception exception,
                            params object[] args) {
        if (LoggingEnabled) {
            _logger.Log(logLevel: logLevel, message: message, exception: exception, args: args);
        }
    }

    private static void Log(LogLevel logLevel, [StructuredMessageTemplate] string message, params object[] args) {
        if (LoggingEnabled) {
            _logger.Log(logLevel: logLevel, message: message, args: args);
        }
    }
}