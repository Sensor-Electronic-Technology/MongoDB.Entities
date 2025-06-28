// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using ConsoleTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities;

Console.WriteLine("Initializing Database...");
await DB.InitAsync(
    "mongodb-test-epidata",
    "172.20.3.41",
    enableLogging: true,
    assemblies: [typeof(EpiRun).Assembly]);

//await GenerateEpiData();
//await TestOneToMany();
//await BuildMigrationNew();
//await DB.ApplyMigrations();
//await BuildEmbeddedMigration();

//await DB.ApplyMigrations();
//await TestAddNewWithCustomFieldData();

/*await AddNewDataWithTestEmbeddedNotArray();
await BuildEmbeddedMigrationNotArray();*/
//await DB.ApplyMigrations();
/*var wafer=await DB.Find<EpiRun>().Match(e => e.WaferId == "B09-9998-96").ExecuteSingleAsync();
Console.WriteLine($"Wafer Found: {wafer.WaferId}");*/

async Task AddNewDataWithTestEmbeddedNotArray() {
    var rand = new Random();
    var now = DateTime.Now;
    EpiRun run = new EpiRun {
        WaferId = "B09-9998-98",
        RunNumber = "9998",
        PocketNumber = "97",
        RunTypeId = "Prod",
        SystemId = "B09",
    };
    run.TestEmbeddedNotArray = new TestEmbeddedNotArray() {
        Name = "TestEmbeddedNotArray",
    };
    await run.SaveAsync();
}

async Task TestAddNewWithCustomFieldData() {
    var rand = new Random();
    var now = DateTime.Now;
    EpiRun run = new EpiRun {
        WaferId = "B09-9998-97",
        RunNumber = "9998",
        PocketNumber = "97",
        RunTypeId = "Prod",
        SystemId = "B09",
    };

    var quickTestData = new QuickTest {
        WaferId = run.WaferId,
        TimeStamp = now,
        InitialMeasurements = new List<QtMeasurement> {
            GenerateQtMeasurement(rand, "A", now),
            GenerateQtMeasurement(rand, "B", now),
            GenerateQtMeasurement(rand, "C", now),
            GenerateQtMeasurement(rand, "L", now),
            GenerateQtMeasurement(rand, "R", now),
            GenerateQtMeasurement(rand, "T", now),
            GenerateQtMeasurement(rand, "G", now)
        },
        FinalMeasurements = new List<QtMeasurement> {
            GenerateQtMeasurement(rand, "A", now),
            GenerateQtMeasurement(rand, "B", now),
            GenerateQtMeasurement(rand, "C", now),
            GenerateQtMeasurement(rand, "L", now),
            GenerateQtMeasurement(rand, "R", now),
            GenerateQtMeasurement(rand, "T", now),
            GenerateQtMeasurement(rand, "G", now)
        }
    };
    var collectionName = DB.CollectionName<QuickTest>();
    var typeConfig = await DB.Collection<TypeConfiguration>()
                             .Find(e => e.CollectionName == collectionName)
                             .FirstOrDefaultAsync();
    var dict = new Dictionary<string, object>() {
        {
            "Qt Summary", new Dictionary<string, object>() {
                { "Power2", 456.76 }, { "Voltage2", 54.2 }
            }
        }
    };

    await run.SaveAsync();

    await quickTestData.SaveMigrateAsync(additionalData: dict);
    run.QuickTest = quickTestData.ToReference();
    quickTestData.EpiRun = run.ToReference();
    await run.SaveAsync();
    await quickTestData.SaveAsync();
}

async Task TestOneToMany() {
    await DB.InitAsync("test_one_to_many", "172.20.3.41");

    //create author and his books
    var author = new Author { Name = "Author One" };
    await author.SaveAsync();

    var books = new[] {
        new Book { Title = "Book One", Author = author.ToReference() },
        new Book { Title = "Book Two", Author = author.ToReference() }
    };

    await books.SaveAsync();

    await author.Books.AddAsync(books);

    //retrieve all books of author

    var allBooks = await author.Books
                               .ChildrenQueryable()
                               .ToListAsync();

    //retrieve first book of author

    var firstBook = await author.Books
                                .ChildrenQueryable()
                                .Where(b => b.Title == "Book One")
                                .ToListAsync();

    //retrieve author together with all of his books

    var authorWithBooks = await DB.Fluent<Author>()
                                  .Match(a => a.ID == author.ID)
                                  .Lookup<Author, Book, AuthorWithBooks>(
                                      DB.Collection<Book>(),
                                      a => a.ID,
                                      b => b.Author.ID,
                                      awb => awb.AllBooks)
                                  .ToListAsync();
    Console.WriteLine(authorWithBooks.Count);
}

async Task UndoRedoAll() {
    Console.WriteLine("Dropping database...");
    await DropAllCollections();
    Console.WriteLine("Database dropped, generating data...");
    await GenerateEpiData();
    Console.WriteLine("Data generated, Adding Migration 1...");
    await BuildMigration();
    Console.WriteLine("Migration 1 created, Migrating...");
    await DB.ApplyMigrations();
    Console.WriteLine("Migration 1 Applied,press any key to continue...");
    Console.ReadKey();
    Console.WriteLine("Adding second migration...");
    await BuildMigration2();
    Console.WriteLine("Migration 2 created, Migrating...");
    await DB.ApplyMigrations();
    Console.WriteLine("Check database, press any key to undo migration 2");
    Console.ReadKey();
    await UndoMigration();
    Console.WriteLine("Check database, press any key to add migration 3");
    Console.ReadKey();
    await BuilderMigration3();
    Console.WriteLine("Migration 3 created, Migrating...");
    await DB.ApplyMigrations();
    Console.WriteLine("Check database");
}

async Task DropAllCollections() {
    await DB.DropCollectionAsync<EpiRun>();
    await DB.DropCollectionAsync<QuickTest>();
    await DB.DropCollectionAsync<XrdData>();
    await DB.DropCollectionAsync<DocumentMigration>();
    await DB.DropCollectionAsync<TypeConfiguration>();
}

async Task BuildEmbeddedMigrationNotArray() {
    var migrationNumber = await DB.Collection<DocumentMigration>()
                                  .Find(_ => true)
                                  .SortByDescending(e => e.MigrationNumber)
                                  .Project(e => e.MigrationNumber)
                                  .FirstOrDefaultAsync();
    Console.WriteLine("Migration Number: " + migrationNumber);

    MigrationBuilder builder = new MigrationBuilder();
    ValueField valueField = new ValueField() {
        FieldName = "AField",
        TypeCode = TypeCode.String,
        BsonType = BsonType.String,
        DataType = DataType.STRING,
        DefaultValue = "DefaultValue"
    };
    builder.AddField(valueField);
    EmbeddedTypeConfiguration? typeConfig =
        EmbeddedTypeConfiguration.CreateOnline<EpiRun,TestEmbeddedNotArray>(
            ["TestEmbeddedNotArray"]);
    if (typeConfig == null) {
        Console.WriteLine("TypeConfiguration.CreateOnline failed!");
        return;
    }
    await typeConfig.SaveAsync();
    var migration = builder.Build(typeConfig, migrationNumber);
    await migration.SaveAsync();

    //migration.TypeConfiguration = typeConfig.ToReference();
    await typeConfig.Migrations.AddAsync(migration);
    await migration.SaveAsync();
    Console.WriteLine("Migration Created");
}

async Task BuildEmbeddedMigration() {
    var migrationNumber = await DB.Collection<DocumentMigration>()
                                  .Find(_ => true)
                                  .SortByDescending(e => e.MigrationNumber)
                                  .Project(e => e.MigrationNumber)
                                  .FirstOrDefaultAsync();
    Console.WriteLine("Migration Number: " + migrationNumber);

    MigrationBuilder builder = new MigrationBuilder();
    ValueField valueField = new ValueField() {
        FieldName = "Wavelength2",
        TypeCode = TypeCode.Double,
        BsonType = BsonType.Double,
        DataType = DataType.NUMBER,
        DefaultValue = 0.00
    };

    ValueField valueField2 = new ValueField() {
        FieldName = "Voltage2",
        TypeCode = TypeCode.Double,
        BsonType = BsonType.Double,
        DataType = DataType.NUMBER,
        DefaultValue = 0.00
    };
    builder.AddField(valueField);
    builder.AddField(valueField2);
    EmbeddedTypeConfiguration? typeConfig =
        EmbeddedTypeConfiguration.CreateOnline<QuickTest, QtMeasurement>(
            ["InitialMeasurements", "FinalMeasurements"],
            true);

    if (typeConfig == null) {
        Console.WriteLine("TypeConfiguration.CreateOnly failed!");
        return;
    }
    await typeConfig.SaveAsync();
    var migration = builder.Build(typeConfig, migrationNumber);
    await migration.SaveAsync();

    //migration.TypeConfiguration = typeConfig.ToReference();
    await typeConfig.Migrations.AddAsync(migration);
    await migration.SaveAsync();
    Console.WriteLine("Migration Created");
}

async Task BuildMigrationNew() {
    var migrationNumber = await DB.Collection<DocumentMigration>()
                                  .Find(_ => true)
                                  .SortByDescending(e => e.MigrationNumber)
                                  .Project(e => e.MigrationNumber)
                                  .FirstOrDefaultAsync();
    Console.WriteLine("Migration Number: " + migrationNumber);
    MigrationBuilder builder = new MigrationBuilder();
    ObjectField objField = new ObjectField() {
        FieldName = "Qt Summary",
        BsonType = BsonType.Document,
        TypeCode = TypeCode.Object,
        Fields = [
            new ValueField() {
                FieldName = "Power2",
                BsonType = BsonType.Double,
                TypeCode = TypeCode.Double,
                DataType = DataType.NUMBER,
                DefaultValue = 0.00
            },
            new ValueField() {
                FieldName = "Voltage2",
                BsonType = BsonType.Double,
                TypeCode = TypeCode.Double,
                DataType = DataType.NUMBER,
                DefaultValue = 0.00
            }
        ]
    };
    builder.AddField(objField);
    TypeConfiguration? typeConfig = TypeConfiguration.CreateOnline<QuickTest>();

    if (typeConfig == null) {
        Console.WriteLine("TypeConfiguration.CreateOnly failed!");

        return;
    }
    await typeConfig.SaveAsync();
    var migration = builder.Build(typeConfig, migrationNumber);
    await migration.SaveAsync();

    //migration.TypeConfiguration = typeConfig.ToReference();
    await typeConfig.Migrations.AddAsync(migration);
    await migration.SaveAsync();
    Console.WriteLine("Migration Created");
}

async Task BuildMigration() {
    var migrationNumber = await DB.Collection<DocumentMigration>()
                                  .Find(_ => true)
                                  .SortByDescending(e => e.MigrationNumber)
                                  .Project(e => e.MigrationNumber)
                                  .FirstOrDefaultAsync();
    Console.WriteLine("Migration Number: " + migrationNumber);

    MigrationBuilder builder = new MigrationBuilder();
    ObjectField objField = new ObjectField {
        FieldName = "Qt Summary",
        BsonType = BsonType.Document,
        TypeCode = TypeCode.Object,
        Fields = [
            new CalculatedField {
                FieldName = "Avg. Initial Power",
                BsonType = BsonType.Double,
                DefaultValue = 0.00,
                Expression = "avg([powers])",
                Variables = [
                    new CollectionPropertyVariable {
                        Property = "Power",
                        VariableName = "powers",
                        CollectionProperty = "InitialMeasurements",
                        Filter = new() {
                            FieldName = nameof(QtMeasurement.Power),
                            CompareOperator = ComparisonOperator.LessThanOrEqual,
                            FilterLogicalOperator = LogicalOperator.And,
                            Value = 1100,
                            Filters = new List<Filter> {
                                new() {
                                    FieldName = nameof(QtMeasurement.Power),
                                    CompareOperator = ComparisonOperator.GreaterThan,
                                    FilterLogicalOperator = LogicalOperator.And,
                                    Value = 500
                                },
                                new() {
                                    FieldName = "Wavelength",
                                    CompareOperator = ComparisonOperator.GreaterThanOrEqual,
                                    FilterLogicalOperator = LogicalOperator.And,
                                    Value = 270,
                                    Filters = new List<Filter> {
                                        new() {
                                            FieldName = "Wavelength",
                                            CompareOperator = ComparisonOperator.LessThanOrEqual,
                                            FilterLogicalOperator = LogicalOperator.Or,
                                            Value = 279
                                        }
                                    }
                                }
                            }
                        },
                        DataType = DataType.LIST_NUMBER
                    }
                ]
            },
            new CalculatedField {
                FieldName = "Avg. Wl",
                BsonType = BsonType.Double,
                DefaultValue = 0.00,
                Expression = "avg([wavelengths])",
                Variables = [
                    new CollectionPropertyVariable {
                        Property = nameof(QtMeasurement.Wavelength),
                        VariableName = "wavelengths",
                        CollectionProperty = nameof(QuickTest.InitialMeasurements),
                        DataType = DataType.LIST_NUMBER,
                        Filter = new() {
                            FieldName = nameof(QtMeasurement.Power),
                            CompareOperator = ComparisonOperator.LessThanOrEqual,
                            FilterLogicalOperator = LogicalOperator.And,
                            Value = 1100,
                            Filters = new List<Filter> {
                                new() {
                                    FieldName = nameof(QtMeasurement.Power),
                                    CompareOperator = ComparisonOperator.GreaterThan,
                                    FilterLogicalOperator = LogicalOperator.And,
                                    Value = 500
                                },
                                new() {
                                    FieldName = "Wavelength",
                                    CompareOperator = ComparisonOperator.GreaterThanOrEqual,
                                    FilterLogicalOperator = LogicalOperator.And,
                                    Value = 270,
                                    Filters = new List<Filter> {
                                        new() {
                                            FieldName = "Wavelength",
                                            CompareOperator = ComparisonOperator.LessThanOrEqual,
                                            FilterLogicalOperator = LogicalOperator.Or,
                                            Value = 279
                                        }
                                    }
                                }
                            }
                        },
                    },
                ]
            }
        ]
    };
    builder.AddField(objField);
    TypeConfiguration? typeConfig = TypeConfiguration.CreateOnline<QuickTest>();

    if (typeConfig == null) {
        Console.WriteLine("TypeConfiguration.CreateOnly failed!");

        return;
    }
    await typeConfig.SaveAsync();
    var migration = builder.Build(typeConfig, migrationNumber);
    await migration.SaveAsync();

    //migration.TypeConfiguration = typeConfig.ToReference();
    await typeConfig.Migrations.AddAsync(migration);
    await migration.SaveAsync();
    Console.WriteLine("Migration Created");
}

async Task BuildMigration2() {
    var migrationNumber = await DB.Collection<DocumentMigration>()
                                  .Find(_ => true)
                                  .SortByDescending(e => e.MigrationNumber)
                                  .Project(e => e.MigrationNumber)
                                  .FirstOrDefaultAsync();
    var collectionName = DB.CollectionName<QuickTest>();
    var typeConfig = await DB.Collection<TypeConfiguration>()
                             .Find(e => e.CollectionName == collectionName)
                             .FirstOrDefaultAsync();

    if (typeConfig == null) {
        Console.WriteLine("TypeConfiguration not found");

        return;
    }
    var field = typeConfig.Fields.FirstOrDefault(e => e.FieldName == "Qt Summary");

    if (field == null) {
        Console.WriteLine("Field not found");

        return;
    }
    var updatedField = FastCloner.FastCloner.DeepClone(field as ObjectField);
    var filter = new Filter {
        FieldName = nameof(QtMeasurement.Power),
        CompareOperator = ComparisonOperator.LessThanOrEqual,
        FilterLogicalOperator = LogicalOperator.And,
        Value = 1100,
        Filters = new List<Filter> {
            new() {
                FieldName = nameof(QtMeasurement.Power),
                CompareOperator = ComparisonOperator.GreaterThan,
                FilterLogicalOperator = LogicalOperator.And,
                Value = 500
            },
            new() {
                FieldName = "Wavelength",
                CompareOperator = ComparisonOperator.GreaterThanOrEqual,
                FilterLogicalOperator = LogicalOperator.And,
                Value = 270,
                Filters = new List<Filter> {
                    new() {
                        FieldName = "Wavelength",
                        CompareOperator = ComparisonOperator.LessThanOrEqual,
                        FilterLogicalOperator = LogicalOperator.Or,
                        Value = 279
                    }
                }
            }
        }
    };

    if (updatedField != null) {
        var voltAvg = new CalculatedField {
            FieldName = "Avg. Voltage",
            BsonType = BsonType.Double,
            DefaultValue = 0.00,
            Expression = "avg([voltages])",
            Variables = [
                new CollectionPropertyVariable {
                    Property = nameof(QtMeasurement.Voltage),
                    VariableName = "voltages",
                    CollectionProperty = nameof(QuickTest.InitialMeasurements),
                    DataType = DataType.LIST_NUMBER,
                    Filter = filter,
                },
            ]
        };
        updatedField.Fields.Add(voltAvg);
        MigrationBuilder builder = new MigrationBuilder();
        builder.AlterField(updatedField, field);
        var migration = builder.Build(typeConfig, migrationNumber);
        await migration.SaveAsync();
        Console.WriteLine("Migration Added");
    } else {
        Console.WriteLine("Error with deep clone");
    }
}

async Task BuilderMigration3() {
    var migrationNumber = await DB.Collection<DocumentMigration>()
                                  .Find(_ => true)
                                  .SortByDescending(e => e.MigrationNumber)
                                  .Project(e => e.MigrationNumber)
                                  .FirstOrDefaultAsync();
    var collectionName = DB.CollectionName<QuickTest>();
    var typeConfig = await DB.Collection<TypeConfiguration>()
                             .Find(e => e.CollectionName == collectionName)
                             .FirstOrDefaultAsync();

    if (typeConfig == null) {
        Console.WriteLine("TypeConfiguration not found");

        return;
    }

    var objField = new ObjectField {
        FieldName = "Qt Pass/Fail",
        BsonType = BsonType.Document,
        TypeCode = TypeCode.Object,
        Fields = [
            new CalculatedField {
                FieldName = "Power Pass/Fail",
                BsonType = BsonType.String,
                IsBooleanExpression = true,
                DefaultValue = "Fail",
                TrueValue = "Pass",
                FalseValue = "Fail",
                Expression = "[pAvg]>[pCriteria]",
                QuantityName = "",
                TypeCode = TypeCode.String,
                Variables = [
                    new ValueVariable {
                        VariableName = "pCriteria",
                        TypeCode = TypeCode.Double,
                        DataType = DataType.NUMBER,
                        Value = 950
                    },
                    new EmbeddedPropertyVariable {
                        VariableName = "pAvg",
                        EmbeddedProperty = "Avg. Initial Power",
                        EmbeddedObjectProperties = ["Qt Summary"],
                        Property = "AdditionalData",
                        DataType = DataType.NUMBER,
                    }
                ]
            },
            new CalculatedField {
                FieldName = "Pass Fail",
                BsonType = BsonType.String,
                IsBooleanExpression = true,
                DefaultValue = "Fail",
                TrueValue = "Pass",
                FalseValue = "Fail",
                Expression = "([pAvg]>[pCriteria]) && ([wlAvg]>=[wlMin] && [wlAvg] <= [wlMax])",
                QuantityName = "",
                TypeCode = TypeCode.String,
                Variables = [
                    new ValueVariable {
                        VariableName = "wlMax",
                        TypeCode = TypeCode.Double,
                        DataType = DataType.NUMBER,
                        Value = 279.5,
                    },
                    new ValueVariable {
                        VariableName = "wlMin",
                        TypeCode = TypeCode.Double,
                        DataType = DataType.NUMBER,
                        Value = 270.5,
                    },
                    new ValueVariable {
                        VariableName = "pCriteria",
                        TypeCode = TypeCode.Double,
                        DataType = DataType.NUMBER,
                        Value = 950
                    },
                    new EmbeddedPropertyVariable {
                        VariableName = "pAvg",
                        EmbeddedProperty = "Avg. Initial Power",
                        EmbeddedObjectProperties = ["Qt Summary"],
                        Property = "AdditionalData",
                        DataType = DataType.NUMBER,
                    },
                    new EmbeddedPropertyVariable {
                        VariableName = "wlAvg",
                        EmbeddedProperty = "Avg. Wl",
                        EmbeddedObjectProperties = ["Qt Summary"],
                        Property = "AdditionalData",
                        DataType = DataType.NUMBER,
                    },
                ]
            }
        ]
    };
    MigrationBuilder builder = new MigrationBuilder();
    builder.AddField(objField);
    var migration = builder.Build(typeConfig, migrationNumber);
    await migration.SaveAsync();
    Console.WriteLine("Migration saved");
}

async Task MigrateOnInsert() {
    var rand = new Random();
    var now = DateTime.Now;
    EpiRun run = new EpiRun {
        WaferId = "B09-9998-96",
        RunNumber = "9998",
        PocketNumber = "97",
        RunTypeId = "Prod",
        SystemId = "B09",
    };

    var quickTestData = new QuickTest {
        WaferId = run.WaferId,
        TimeStamp = now,
        InitialMeasurements = new List<QtMeasurement> {
            GenerateQtMeasurement(rand, "A", now),
            GenerateQtMeasurement(rand, "B", now),
            GenerateQtMeasurement(rand, "C", now),
            GenerateQtMeasurement(rand, "L", now),
            GenerateQtMeasurement(rand, "R", now),
            GenerateQtMeasurement(rand, "T", now),
            GenerateQtMeasurement(rand, "G", now)
        },
        FinalMeasurements = new List<QtMeasurement> {
            GenerateQtMeasurement(rand, "A", now),
            GenerateQtMeasurement(rand, "B", now),
            GenerateQtMeasurement(rand, "C", now),
            GenerateQtMeasurement(rand, "L", now),
            GenerateQtMeasurement(rand, "R", now),
            GenerateQtMeasurement(rand, "T", now),
            GenerateQtMeasurement(rand, "G", now)
        }
    };
    await run.SaveAsync();
    await quickTestData.SaveAsync();
    run.QuickTest = quickTestData.ToReference();
    quickTestData.EpiRun = run.ToReference();
    await run.SaveMigrateAsync();
    await quickTestData.SaveMigrateAsync();
}

async Task UndoMigration(int number = 2) {
    var migration = await DB.Collection<DocumentMigration>().Find(e => e.MigrationNumber == 2)
                            .FirstOrDefaultAsync();

    if (migration != null) {
        Console.WriteLine($"Reverting migration {migration.MigrationNumber}");
        await DB.RevertMigration(migration);
        Console.WriteLine("Migration undone, check database");
    } else {
        Console.WriteLine("Migration not found");
    }
}

async Task CheckMigrationConflicts() {
    MigrationBuilder builder = new MigrationBuilder();
    ObjectField objField = new ObjectField {
        FieldName = "Qt Summary",
        BsonType = BsonType.Document,
        TypeCode = TypeCode.Object,
        Fields = [
            new CalculatedField {
                FieldName = "Avg. Initial Power",
                BsonType = BsonType.Double,
                DefaultValue = 0.00,
                Expression = "avg([powers])",
                Variables = [
                    new CollectionPropertyVariable {
                        Property = "Power",
                        VariableName = "powers",
                        CollectionProperty = "InitialMeasurements",
                        Filter = new() {
                            FieldName = nameof(QtMeasurement.Power),
                            CompareOperator = ComparisonOperator.LessThanOrEqual,
                            FilterLogicalOperator = LogicalOperator.And,
                            Value = 1100,
                            Filters = new List<Filter> {
                                new() {
                                    FieldName = nameof(QtMeasurement.Power),
                                    CompareOperator = ComparisonOperator.GreaterThan,
                                    FilterLogicalOperator = LogicalOperator.And,
                                    Value = 500
                                },
                                new() {
                                    FieldName = "Wavelength",
                                    CompareOperator = ComparisonOperator.GreaterThanOrEqual,
                                    FilterLogicalOperator = LogicalOperator.Or,
                                    Value = 270,
                                    Filters = new List<Filter> {
                                        new() {
                                            FieldName = "Wavelength",
                                            CompareOperator = ComparisonOperator.LessThanOrEqual,
                                            FilterLogicalOperator = LogicalOperator.Or,
                                            Value = 279
                                        }
                                    }
                                }
                            }
                        },
                        DataType = DataType.LIST_NUMBER
                    }
                ]
            },
            new CalculatedField {
                FieldName = "Avg. Wl",
                BsonType = BsonType.Double,
                DefaultValue = 0.00,
                Expression = "avg([wavelengths])",
                Variables = [
                    new CollectionPropertyVariable {
                        Property = "Wavelength",
                        VariableName = "wavelengths",
                        CollectionProperty = "Measurements",
                        DataType = DataType.LIST_NUMBER,
                        Filter = new() {
                            FieldName = nameof(QtMeasurement.Area),
                            CompareOperator = ComparisonOperator.LessThanOrEqual,
                            FilterLogicalOperator = LogicalOperator.And,
                            Value = 1100,
                            Filters = new List<Filter> {
                                new() {
                                    FieldName = nameof(QtMeasurement.Power),
                                    CompareOperator = ComparisonOperator.GreaterThan,
                                    FilterLogicalOperator = LogicalOperator.And,
                                    Value = 100
                                },
                                new() {
                                    FieldName = "Wavelength",
                                    CompareOperator = ComparisonOperator.GreaterThanOrEqual,
                                    FilterLogicalOperator = LogicalOperator.Or,
                                    Value = 270,
                                    Filters = new List<Filter> {
                                        new() {
                                            FieldName = "Wavelength",
                                            CompareOperator = ComparisonOperator.LessThanOrEqual,
                                            FilterLogicalOperator = LogicalOperator.Or,
                                            Value = 279
                                        }
                                    }
                                }
                            }
                        },
                    },
                ]
            }
        ]
    };
    var cursor = await DB.Collection<DocumentMigration>().FindAsync(_ => true);

    while (await cursor.MoveNextAsync()) {
        foreach (var migration in cursor.Current) { }
    }
}

async Task TestSubFilter() {
    var runCollection = DB.Collection<EpiRun>();
    var waferIds = await runCollection.AsQueryable().Where(e => e.TechnicianId == "NC").Select(e => e.WaferId)
                                      .ToListAsync();
    var collection = DB.Collection("epi_system", "quick_tests");
    var list = await collection.AsQueryable().Where(e => waferIds.Contains(e["WaferId"].AsString)).ToListAsync();
    var powers = list.SelectMany(e => e["InitialMeasurements"].AsBsonArray)
                     .Select(e => e["Power"].AsDouble).Average();
    Console.WriteLine(powers);
    /*var list = collection.AsQueryable().Where(e => waferIds.Contains(e["WaferId"].AsString))
                         .Select<BsonArray>("e=>e.InitialMeasurements");*/

    /*foreach(var id in waferIds) {
        /*var measurements = await DB.Collection<QuickTest>()
                                   .AsQueryable()
                                   .Where(e=>e.WaferId==id)
                                   .SelectMany(e=>e.InitialMeasurements).ToListAsync();
        var avg=measurements.Select(e=>e.Power).Average();
        Console.WriteLine($"{id}: {avg}");#1#
        var collection=DB.Collection("epi_system","quick_tests");
        var list =await collection.AsQueryable().Where(e => e["WaferId"].AsString == id).ToListAsync();
        var avg=list.SelectMany(e=>e["InitialMeasurements"].AsBsonArray)
                    .Select(e=>e["Power"].AsDouble).Average();


        Console.WriteLine($"{id}: {avg}");
        /*foreach(var power in powers) {
            var array = power.AsBsonArray;
            array.Select(e => e["Power"].AsDouble).Average();
        }#1#
    }*/
}

async Task TestBuilderFilter() {
    var filter = new Filter {
        FieldName = nameof(QtMeasurement.Power),
        CompareOperator = ComparisonOperator.LessThanOrEqual,
        FilterLogicalOperator = LogicalOperator.And,
        Value = 1300,
        Filters = new List<Filter> {
            new Filter {
                FieldName = nameof(QtMeasurement.Power),
                CompareOperator = ComparisonOperator.GreaterThan,
                FilterLogicalOperator = LogicalOperator.And,
                Value = 100
            },
            new Filter {
                FieldName = "Wavelength",
                CompareOperator = ComparisonOperator.GreaterThanOrEqual,
                FilterLogicalOperator = LogicalOperator.Or,
                Value = 275,
                Filters = new List<Filter> {
                    new Filter {
                        FieldName = "Wavelength",
                        CompareOperator = ComparisonOperator.LessThanOrEqual,
                        FilterLogicalOperator = LogicalOperator.Or,
                        Value = 278
                    }
                }
            }
        }
    };
    Console.WriteLine(filter.ToString());
    /*var cursorAsync = await DB.Entity<QuickTest>().Fluent().ToCursorAsync();
    while (await cursorAsync.MoveNextAsync()) {
        foreach (var item in cursorAsync.Current) {
            var measurements = item.InitialMeasurements.AsQueryable().Where(filter.ToString()).Select(e => e.Power).ToList();
            if (measurements.Any()) {
                Console.WriteLine(JsonSerializer.Serialize(measurements));
            }
        }
    }*/
}

async Task GenerateEpiData() {
    var rand = new Random();
    var now = DateTime.Now;
    List<EpiRun> epiRuns = [];
    List<QuickTest> quickTests = [];
    List<XrdData> xrdMeasurementData = [];

    for (int i = 1; i <= 10; i++) {
        for (int x = 1; x <= 10; x++) {
            EpiRun run = new EpiRun {
                RunTypeId = (rand.NextDouble() > .5) ? "Prod" : "Rnd",
                SystemId = "B01",
                TechnicianId = (rand.NextDouble() > .5) ? "RJ" : "NC",
            };
            run.TimeStamp = now;

            string tempId = "";
            string ledId = "";
            string rlId = "";
            GenerateWaferIds(i, "A03", "A02", "B01", ref tempId, ref rlId, ref ledId);

            run.RunNumber = ledId.Substring(ledId.LastIndexOf('-') + 1);

            string tempId_P = tempId;
            string ledId_P = ledId;
            string rlId_P = rlId;

            if (x / 10 >= 1) {
                tempId_P += $"-{x}";
                rlId_P += $"-{x}";
                ledId_P += $"-{x}";
                run.PocketNumber = $"{x}";
            } else {
                tempId_P += $"-0{x}";
                rlId_P += $"-0{x}";
                ledId_P += $"-0{x}";
                run.PocketNumber = $"0{x}";
            }
            run.WaferId = ledId_P;
            epiRuns.Add(run);
            /*await run.SaveAsync();*/

            var quickTestData = new QuickTest {
                WaferId = run.WaferId,

                TimeStamp = now,
                InitialMeasurements = new List<QtMeasurement> {
                    GenerateQtMeasurement(rand, "A", now),
                    GenerateQtMeasurement(rand, "B", now),
                    GenerateQtMeasurement(rand, "C", now),
                    GenerateQtMeasurement(rand, "L", now),
                    GenerateQtMeasurement(rand, "R", now),
                    GenerateQtMeasurement(rand, "T", now),
                    GenerateQtMeasurement(rand, "G", now)
                },
                FinalMeasurements = new List<QtMeasurement> {
                    GenerateQtMeasurement(rand, "A", now),
                    GenerateQtMeasurement(rand, "B", now),
                    GenerateQtMeasurement(rand, "C", now),
                    GenerateQtMeasurement(rand, "L", now),
                    GenerateQtMeasurement(rand, "R", now),
                    GenerateQtMeasurement(rand, "T", now),
                    GenerateQtMeasurement(rand, "G", now)
                }
            };
            quickTests.Add(quickTestData);

            /*await quickTestData.SaveAsync();
            await run.QuickTests.AddAsync(quickTestData);*/
            var xrdData = new XrdData {
                WaferId = run.WaferId,
                XrdMeasurements = new List<XrdMeasurement> {
                    GenerateXrdMeasurement(rand, "C", now),
                    GenerateXrdMeasurement(rand, "Edge", now)
                }
            };
            xrdMeasurementData.Add(xrdData);
            /*await xrdMeasurements.SaveAsync();
            await run.XrdMeasurements.AddAsync(xrdMeasurements);*/
        } //end pocked for loop
    }     //end run number for loop
    await epiRuns.SaveAsync();
    await quickTests.SaveAsync();
    await xrdMeasurementData.SaveAsync();

    epiRuns.ForEach(run => {
        var qt = quickTests.FirstOrDefault(e => e.WaferId == run.WaferId);
        var xrd = xrdMeasurementData.FirstOrDefault(e => e.WaferId == run.WaferId);

        if (qt != null) {
            run.QuickTest = qt.ToReference();
            qt.EpiRun = run.ToReference();
        }

        if (xrd != null) {
            run.XrdData = xrd.ToReference();
            xrd.EpiRun = run.ToReference();
        }
    });

    await epiRuns.SaveAsync();
    await quickTests.SaveAsync();
    await xrdMeasurementData.SaveAsync();
    Console.WriteLine("Check Database");
}

XrdMeasurement GenerateXrdMeasurement(Random rand, string Area, DateTime now) {
    var xrd = new XrdMeasurement {
        XrdArea = Area,
        TimeStamp = now,
        Alpha_AlN = NextDouble(rand, 35.937, 36.0211),
        Beta_AlN = NextDouble(rand, 35.9472, 36.0754),
        FHWM102 = NextDouble(rand, 180, 568.8),
        FWHM002 = NextDouble(rand, 7.2, 358.8),
        dOmega = NextDouble(rand, 0.0065, .3748),
        Omega = NextDouble(rand, 16.2183, 18.3815)
    };

    return xrd;
}

QtMeasurement GenerateQtMeasurement(Random rand, string Area, DateTime now) {
    var qt = new QtMeasurement {
        Area = Area,
        TimeStamp = now,
        Current = 20.0,
        Power = NextDouble(rand, 700, 1700),
        Voltage = NextDouble(rand, 9.5, 15.5),
        Wavelength = NextDouble(rand, 270, 279.9)
    };

    return qt;
}

void GenerateWaferIds(int i,
                      string tempSystem,
                      string rlSystem,
                      string ledSystem,
                      ref string tempId,
                      ref string rlId,
                      ref string ledId) {
    tempId = tempSystem;
    rlId = rlSystem;
    ledId = ledSystem;

    if (i / 1000 >= 1) {
        tempId += $"-{i}";
        rlId += $"-{i}";
        ledId += $"-{i}";
    } else if (i / 100 >= 1) {
        tempId += $"-0{i}";
        rlId += $"-0{i}";
        ledId += $"-0{i}";
    } else if (i / 10 >= 1) {
        tempId += $"-00{i}";
        rlId += $"-00{i}";
        ledId += $"-00{i}";
    } else {
        tempId += $"-000{i}";
        rlId += $"-000{i}";
        ledId += $"-000{i}";
    }
}

double NextDouble(Random rand, double min, double max)
    => rand.NextDouble() * (max - min) + min;

async Task TestTypeConfigAvailableProperties() {
    ObjectField objField = new ObjectField {
        FieldName = "Qt Summary",
        BsonType = BsonType.Document,
        TypeCode = TypeCode.Object,
        Fields = [
            new CalculatedField {
                FieldName = "Avg. Initial Power",
                BsonType = BsonType.Double,
                TypeCode = TypeCode.Double,
                DefaultValue = 0.00,
                Expression = "avg([powers])",
                Variables = [
                    new CollectionPropertyVariable {
                        Property = "Power",
                        VariableName = "powers",
                        CollectionProperty = "InitialMeasurements",
                        Filter = new() {
                            FieldName = nameof(QtMeasurement.Power),
                            CompareOperator = ComparisonOperator.LessThanOrEqual,
                            FilterLogicalOperator = LogicalOperator.And,
                            Value = 1100,
                            Filters = new List<Filter> {
                                new() {
                                    FieldName = nameof(QtMeasurement.Power),
                                    CompareOperator = ComparisonOperator.GreaterThan,
                                    FilterLogicalOperator = LogicalOperator.And,
                                    Value = 500
                                },
                                new() {
                                    FieldName = "Wavelength",
                                    CompareOperator = ComparisonOperator.GreaterThanOrEqual,
                                    FilterLogicalOperator = LogicalOperator.And,
                                    Value = 270,
                                    Filters = new List<Filter> {
                                        new() {
                                            FieldName = "Wavelength",
                                            CompareOperator = ComparisonOperator.LessThanOrEqual,
                                            FilterLogicalOperator = LogicalOperator.Or,
                                            Value = 279
                                        }
                                    }
                                }
                            }
                        },
                        DataType = DataType.LIST_NUMBER
                    }
                ]
            },
            new CalculatedField {
                FieldName = "Avg. Wl",
                BsonType = BsonType.Double,
                TypeCode = TypeCode.Double,
                DefaultValue = 0.00,
                Expression = "avg([wavelengths])",
                Variables = [
                    new CollectionPropertyVariable {
                        Property = nameof(QtMeasurement.Wavelength),
                        VariableName = "wavelengths",
                        CollectionProperty = nameof(QuickTest.InitialMeasurements),
                        DataType = DataType.LIST_NUMBER,
                        Filter = new() {
                            FieldName = nameof(QtMeasurement.Power),
                            CompareOperator = ComparisonOperator.LessThanOrEqual,
                            FilterLogicalOperator = LogicalOperator.And,
                            Value = 1100,
                            Filters = new List<Filter> {
                                new() {
                                    FieldName = nameof(QtMeasurement.Power),
                                    CompareOperator = ComparisonOperator.GreaterThan,
                                    FilterLogicalOperator = LogicalOperator.And,
                                    Value = 500
                                },
                                new() {
                                    FieldName = "Wavelength",
                                    CompareOperator = ComparisonOperator.GreaterThanOrEqual,
                                    FilterLogicalOperator = LogicalOperator.And,
                                    Value = 270,
                                    Filters = new List<Filter> {
                                        new() {
                                            FieldName = "Wavelength",
                                            CompareOperator = ComparisonOperator.LessThanOrEqual,
                                            FilterLogicalOperator = LogicalOperator.Or,
                                            Value = 279
                                        }
                                    }
                                }
                            }
                        },
                    },
                ]
            }
        ]
    };
    var typeConfig = TypeConfiguration.CreateOnline<EpiRun>();

    if (typeConfig != null) {
        typeConfig.Fields.Add(objField);
        typeConfig.UpdateAvailableProperties();
        await typeConfig.SaveAsync();
        Console.WriteLine("Check database");
    } else {
        Console.WriteLine("Error TypeConfiguration.Create failed");
    }
}

public class TestCreated : DocumentEntity {
    public string? WaferId { get; set; }

    public Many<TestMany, TestCreated> TestManys { get; set; }

    /*public object GenerateNewID()
        => string.Empty;

    public bool HasDefaultID()
        => false;

    public BsonDocument? AdditionalData { get; set; }
    public DocumentVersion Version { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }*/

    public TestCreated() {
        this.InitOneToMany(() => this.TestManys);
    }
}

public class TestMany : DocumentEntity {
    public string? SomeId { get; set; }
    public One<TestCreated>? TestCreated { get; set; }
}

public class Author : Entity {
    public string Name { get; set; }
    public Many<Book, Author> Books { get; set; }

    public Author()
        => this.InitOneToMany(() => Books);
}

public class Book : Entity {
    public string Title { get; set; }
    public One<Author> Author { get; set; }
}

public class AuthorWithBooks : Author {
    public Book[] AllBooks { get; set; }
}