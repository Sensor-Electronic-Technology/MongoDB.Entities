// See https://aka.ms/new-console-template for more information

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Parser;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.RegularExpressions;
using ConsoleTesting;
using Extensions;
using MongoDB.Driver.Linq;
using NCalcExtensions;
using FastCloner;
using CollectionPropertyVariable = MongoDB.Entities.CollectionPropertyVariable;
using VariableType = MongoDB.Entities.VariableType;

await DB.InitAsync("epi_system", "172.20.3.41");
//await UndoRedoAll();

string expression = "([pAvg]>=[pCritera]) && ([wlAvg]>=[wlMin] && [wlMax]>=[wlAvg])";

/*value.Count(c=>c=='[');
value.Count(c=>c==']');*/
/*string[] splitResult = Regex.Split(value, @"\[(.*?)\]",RegexOptions.Compiled);
Console.WriteLine(JsonSerializer.Serialize(splitResult));*/


var matches = Regex.Matches(expression, @"\[(.*?)\]", RegexOptions.Compiled);
foreach (Match match in matches)
{
    string content = match.Groups[1].Value;
    Console.WriteLine($"Content between brackets: {content}");
    // Perform any additional processing on the content here
}

async Task UndoRedoAll() {
    Console.WriteLine("Dropping database...");
    await DB.DropCollectionAsync<EpiRun>();
    await DB.DropCollectionAsync<QuickTest>();
    await DB.DropCollectionAsync<XrdData>();
    await DB.DropCollectionAsync<DocumentMigration>();
    await DB.DropCollectionAsync<TypeConfiguration>();
    Console.WriteLine("Database dropped, generating data...");
    await GenerateEpiData();
    Console.WriteLine("Data generated, Adding Migration 1...");
    await BuildMigration();
    Console.WriteLine("Migration 1 created, Migrating...");
    await DB.MigrateFields();
    Console.WriteLine("Migration 1 Applied, Adding second migration...");
    await BuildMigration2();
    Console.WriteLine("Migration 2 created, Migrating...");
    await DB.MigrateFields();
    Console.WriteLine("Check database, press any key to undo migration 2");
    Console.ReadKey();
    await UndoMigration();
}

async Task UndoMigration() {
    var migration = await DB.Collection<DocumentMigration>().Find(e=>e.MigrationNumber==2)
                            .FirstOrDefaultAsync();
    if (migration != null) {
        Console.WriteLine($"Reverting migration {migration.MigrationNumber}");
        await DB.UndoMigration(migration);
        Console.WriteLine("Migration undone, check database");
    } else {
        Console.WriteLine("Migration not found");
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

    var objField = new ObjectField() {
        FieldName = "Pass Fail",
        BsonType=BsonType.Document,
        TypeCode=TypeCode.Object,
        Fields = [
            new CalculatedField() {
                FieldName = "Power Result",
                BsonType=BsonType.Double,
                DefaultValue = "Fail",
                Expression = "[pAvg>[pCriteria]]",
                QuantityName = "",
                TypeCode = TypeCode.String,
                
            }
        ]
    };
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
    var field = typeConfig.Fields[0];
    var updatedField = FastCloner.FastCloner.DeepClone(field as ObjectField);
    if (updatedField != null) {
        var voltAvg = new CalculatedField() {
            FieldName = "Avg. Voltage",
            BsonType = BsonType.Double,
            DefaultValue = 0.00,
            Expression = "avg([voltages])",
            Variables = [
                new CollectionPropertyVariable() {
                    Property = nameof(QtMeasurement.Voltage),
                    VariableName = "voltages",
                    CollectionProperty = nameof(QuickTest.InitialMeasurements),
                    VariableType = VariableType.LIST_NUMBER,
                    Filter = new() {
                        FieldName = nameof(QtMeasurement.Power),
                        ComparisonOperator = ComparisonOperator.LessThanOrEqual,
                        LogicalOperator = LogicalOperator.And,
                        Value = 1100,
                        Filters = new List<Filter>() {
                            new() {
                                FieldName = nameof(QtMeasurement.Power),
                                ComparisonOperator = ComparisonOperator.GreaterThan,
                                LogicalOperator = LogicalOperator.And,
                                Value = 500
                            },
                            new() {
                                FieldName = "Wavelength",
                                ComparisonOperator = ComparisonOperator.GreaterThanOrEqual,
                                LogicalOperator = LogicalOperator.And,
                                Value = 270,
                                Filters = new List<Filter>() {
                                    new() {
                                        FieldName = "Wavelength",
                                        ComparisonOperator = ComparisonOperator.LessThanOrEqual,
                                        LogicalOperator = LogicalOperator.Or,
                                        Value = 279
                                    }
                                }
                            }
                        }
                    },
                },
            ]
        };
        updatedField.Fields.Add(voltAvg);
        MigrationBuilder builder = new MigrationBuilder();
        builder.AlterField(updatedField, field);
        var migration = builder.Build();
        migration.MigrationNumber = ++migrationNumber;
        migration.TypeConfiguration = typeConfig.ToReference();
        await migration.SaveAsync();
        Console.WriteLine("Migration Added");
    } else {
        Console.WriteLine("Error with deep clone");
    }
}

async Task CheckMigrationConflicts() {
    MigrationBuilder builder = new MigrationBuilder();
    ObjectField objField = new ObjectField() {
        FieldName = "Qt Summary",
        BsonType = BsonType.Document,
        TypeCode = TypeCode.Object,
        Fields = [
            new CalculatedField() {
                FieldName = "Avg. Initial Power",
                BsonType = BsonType.Double,
                DefaultValue = 0.00,
                Expression = "avg([powers])",
                Variables = [
                    new CollectionPropertyVariable() {
                        Property = "Power",
                        VariableName = "powers",
                        CollectionProperty = "InitialMeasurements",
                        Filter = new() {
                            FieldName = nameof(QtMeasurement.Power),
                            ComparisonOperator = ComparisonOperator.LessThanOrEqual,
                            LogicalOperator = LogicalOperator.And,
                            Value = 1100,
                            Filters = new List<Filter>() {
                                new() {
                                    FieldName = nameof(QtMeasurement.Power),
                                    ComparisonOperator = ComparisonOperator.GreaterThan,
                                    LogicalOperator = LogicalOperator.And,
                                    Value = 500
                                },
                                new() {
                                    FieldName = "Wavelength",
                                    ComparisonOperator = ComparisonOperator.GreaterThanOrEqual,
                                    LogicalOperator = LogicalOperator.Or,
                                    Value = 270,
                                    Filters = new List<Filter>() {
                                        new() {
                                            FieldName = "Wavelength",
                                            ComparisonOperator = ComparisonOperator.LessThanOrEqual,
                                            LogicalOperator = LogicalOperator.Or,
                                            Value = 279
                                        }
                                    }
                                }
                            }
                        },
                        VariableType = VariableType.LIST_NUMBER
                    }
                ]
            },
            new CalculatedField() {
                FieldName = "Avg. Wl",
                BsonType = BsonType.Double,
                DefaultValue = 0.00,
                Expression = "avg([wavelengths])",
                Variables = [
                    new CollectionPropertyVariable() {
                        Property = "Wavelength",
                        VariableName = "wavelengths",
                        CollectionProperty = "Measurements",
                        VariableType = VariableType.LIST_NUMBER,
                        Filter = new() {
                            FieldName = nameof(QtMeasurement.Area),
                            ComparisonOperator = ComparisonOperator.LessThanOrEqual,
                            LogicalOperator = LogicalOperator.And,
                            Value = 1100,
                            Filters = new List<Filter>() {
                                new() {
                                    FieldName = nameof(QtMeasurement.Power),
                                    ComparisonOperator = ComparisonOperator.GreaterThan,
                                    LogicalOperator = LogicalOperator.And,
                                    Value = 100
                                },
                                new() {
                                    FieldName = "Wavelength",
                                    ComparisonOperator = ComparisonOperator.GreaterThanOrEqual,
                                    LogicalOperator = LogicalOperator.Or,
                                    Value = 270,
                                    Filters = new List<Filter>() {
                                        new() {
                                            FieldName = "Wavelength",
                                            ComparisonOperator = ComparisonOperator.LessThanOrEqual,
                                            LogicalOperator = LogicalOperator.Or,
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

async Task BuildMigration() {
    //DB.Find<DocumentMigration, int>().Match(_ => true).Project(e => e.MigrationNumber;
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
            new CalculatedField() {
                FieldName = "Avg. Initial Power",
                BsonType = BsonType.Double,
                DefaultValue = 0.00,
                Expression = "avg([powers])",
                Variables = [
                    new CollectionPropertyVariable() {
                        Property = "Power",
                        VariableName = "powers",
                        CollectionProperty = "InitialMeasurements",
                        Filter = new() {
                            FieldName = nameof(QtMeasurement.Power),
                            ComparisonOperator = ComparisonOperator.LessThanOrEqual,
                            LogicalOperator = LogicalOperator.And,
                            Value = 1100,
                            Filters = new List<Filter>() {
                                new() {
                                    FieldName = nameof(QtMeasurement.Power),
                                    ComparisonOperator = ComparisonOperator.GreaterThan,
                                    LogicalOperator = LogicalOperator.And,
                                    Value = 500
                                },
                                new() {
                                    FieldName = "Wavelength",
                                    ComparisonOperator = ComparisonOperator.GreaterThanOrEqual,
                                    LogicalOperator = LogicalOperator.And,
                                    Value = 270,
                                    Filters = new List<Filter>() {
                                        new() {
                                            FieldName = "Wavelength",
                                            ComparisonOperator = ComparisonOperator.LessThanOrEqual,
                                            LogicalOperator = LogicalOperator.Or,
                                            Value = 279
                                        }
                                    }
                                }
                            }
                        },
                        VariableType = VariableType.LIST_NUMBER
                    }
                ]
            },
            new CalculatedField() {
                FieldName = "Avg. Wl",
                BsonType = BsonType.Double,
                DefaultValue = 0.00,
                Expression = "avg([wavelengths])",
                Variables = [
                    new CollectionPropertyVariable() {
                        Property = nameof(QtMeasurement.Wavelength),
                        VariableName = "wavelengths",
                        CollectionProperty = nameof(QuickTest.InitialMeasurements),
                        VariableType = VariableType.LIST_NUMBER,
                        Filter = new() {
                            FieldName = nameof(QtMeasurement.Power),
                            ComparisonOperator = ComparisonOperator.LessThanOrEqual,
                            LogicalOperator = LogicalOperator.And,
                            Value = 1100,
                            Filters = new List<Filter>() {
                                new() {
                                    FieldName = nameof(QtMeasurement.Power),
                                    ComparisonOperator = ComparisonOperator.GreaterThan,
                                    LogicalOperator = LogicalOperator.And,
                                    Value = 500
                                },
                                new() {
                                    FieldName = "Wavelength",
                                    ComparisonOperator = ComparisonOperator.GreaterThanOrEqual,
                                    LogicalOperator = LogicalOperator.And,
                                    Value = 270,
                                    Filters = new List<Filter>() {
                                        new() {
                                            FieldName = "Wavelength",
                                            ComparisonOperator = ComparisonOperator.LessThanOrEqual,
                                            LogicalOperator = LogicalOperator.Or,
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
    TypeConfiguration typeConfig = new TypeConfiguration() {
        CollectionName = DB.CollectionName<QuickTest>(),
        DatabaseName = DB.DatabaseName<QuickTest>(),
    };
    var migration = builder.Build();
    migration.MigrationNumber = ++migrationNumber;
    await migration.SaveAsync();
    await typeConfig.SaveAsync();
    migration.TypeConfiguration = typeConfig.ToReference();
    await typeConfig.Migrations.AddAsync(migration);

    //await typeConfig.SaveAsync();
    await migration.SaveAsync();
    Console.WriteLine("Migration Created");
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
    var filter = new Filter() {
        FieldName = nameof(QtMeasurement.Power),
        ComparisonOperator = ComparisonOperator.LessThanOrEqual,
        LogicalOperator = LogicalOperator.And,
        Value = 1300,
        Filters = new List<Filter>() {
            new Filter() {
                FieldName = nameof(QtMeasurement.Power),
                ComparisonOperator = ComparisonOperator.GreaterThan,
                LogicalOperator = LogicalOperator.And,
                Value = 100
            },
            new Filter() {
                FieldName = "Wavelength",
                ComparisonOperator = ComparisonOperator.GreaterThanOrEqual,
                LogicalOperator = LogicalOperator.Or,
                Value = 275,
                Filters = new List<Filter>() {
                    new Filter() {
                        FieldName = "Wavelength",
                        ComparisonOperator = ComparisonOperator.LessThanOrEqual,
                        LogicalOperator = LogicalOperator.Or,
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

async Task TestDynamicScript() {
    ObjectField objField = new ObjectField() {
        FieldName = "Qt Summary",
        BsonType = BsonType.Document,
        TypeCode = TypeCode.Object,
        Fields = [
            new CalculatedField() {
                FieldName = "Avg. Initial Power",
                BsonType = BsonType.Double,
                DefaultValue = 0.00,
                Expression = "avg([powers])",
                Variables = [
                    new CollectionPropertyVariable() {
                        Property = "Power",
                        VariableName = "powers",
                        CollectionProperty = "InitialMeasurements",
                        Filter = new() {
                            FieldName = nameof(QtMeasurement.Area),
                            ComparisonOperator = ComparisonOperator.LessThanOrEqual,
                            LogicalOperator = LogicalOperator.And,
                            Value = 1100,
                            Filters = new List<Filter>() {
                                new() {
                                    FieldName = nameof(QtMeasurement.Power),
                                    ComparisonOperator = ComparisonOperator.GreaterThan,
                                    LogicalOperator = LogicalOperator.And,
                                    Value = 900
                                },
                                new() {
                                    FieldName = "Wavelength",
                                    ComparisonOperator = ComparisonOperator.GreaterThanOrEqual,
                                    LogicalOperator = LogicalOperator.Or,
                                    Value = 275,
                                    Filters = new List<Filter>() {
                                        new() {
                                            FieldName = "Wavelength",
                                            ComparisonOperator = ComparisonOperator.LessThanOrEqual,
                                            LogicalOperator = LogicalOperator.Or,
                                            Value = 278
                                        }
                                    }
                                }
                            }
                        },
                        VariableType = VariableType.LIST_NUMBER
                    }
                ]
            },
            new CalculatedField() {
                FieldName = "Avg. Wl",
                BsonType = BsonType.Double,
                DefaultValue = 0.00,
                Expression = "avg([wavelengths])",
                Variables = [
                    new CollectionPropertyVariable() {
                        Property = "Wavelength",
                        VariableName = "wavelengths",
                        CollectionProperty = "Measurements",
                        VariableType = VariableType.LIST_NUMBER,
                        Filter = new() {
                            FieldName = nameof(QtMeasurement.Area),
                            ComparisonOperator = ComparisonOperator.LessThanOrEqual,
                            LogicalOperator = LogicalOperator.And,
                            Value = 1100,
                            Filters = new List<Filter>() {
                                new() {
                                    FieldName = nameof(QtMeasurement.Power),
                                    ComparisonOperator = ComparisonOperator.GreaterThan,
                                    LogicalOperator = LogicalOperator.And,
                                    Value = 500
                                },
                                new() {
                                    FieldName = "Wavelength",
                                    ComparisonOperator = ComparisonOperator.GreaterThanOrEqual,
                                    LogicalOperator = LogicalOperator.Or,
                                    Value = 275,
                                    Filters = new List<Filter>() {
                                        new() {
                                            FieldName = "Wavelength",
                                            ComparisonOperator = ComparisonOperator.LessThanOrEqual,
                                            LogicalOperator = LogicalOperator.Or,
                                            Value = 278
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
    var pField = objField.Fields[0] as CalculatedField;
    /*var script = CSharpScript.Create<double>($"QueryObject.{((CollectionVariable)pField.Variables[0]).CollectionProperty}.Select(e => e.{((CollectionVariable)pField.Variables[0]).Property});",
    globalsType:typeof(ScriptInput<QuickTest>),
    options:ScriptOptions.Default
                         .WithReferences(typeof(DB).Assembly, typeof(EpiRun).Assembly, typeof(Monitoring).Assembly)
                         .WithImports("MongoDB.Entities","MongoDB.Driver", "System.Linq"));
    script.Compile();*/
    var dataCollect = DB.Collection("epi_system", "quick_tests");
    var cursor = await dataCollect.Find(_ => true).ToCursorAsync();
    var collectionVariable = ((CollectionPropertyVariable)pField.Variables[0]);
    var expression = new ExtendedExpression(pField.Expression);

    while (await cursor.MoveNextAsync()) {
        foreach (var doc in cursor.Current) {
            List<object> list = [];

            if (doc.Contains(collectionVariable.CollectionProperty)) {
                switch (collectionVariable.VariableType) {
                    case VariableType.NUMBER: {
                        //Where($"e=>e.{collectionVariable.Property}>800 && e.{collectionVariable.Property}<1300") .Where(FilterParser.FilterToString(collectionVariable.Filter))
                        var dList = doc[collectionVariable.CollectionProperty].AsBsonArray.AsQueryable()
                                                                              .Where(
                                                                                  collectionVariable.Filter
                                                                                      ?.ToString() ??
                                                                                  "")
                                                                              .Select(
                                                                                  $"e=>e.{collectionVariable.Property}.AsDouble");
                        expression.Parameters[collectionVariable.VariableName] = dList;

                        if (dList.Any()) {
                            Console.WriteLine(expression.Evaluate());
                        }

                        break;
                    }

                    case VariableType.STRING:

                        break;
                    case VariableType.BOOLEAN:

                        break;
                    case VariableType.DATE:

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}

async Task CreateTypeConfiguration() {
    /*TypeConfigurationMap configMap = new TypeConfigurationMap() {
        CollectionName = DB.CollectionName<QuickTest>(),
    };*/

    TypeConfiguration config = new TypeConfiguration() {
        CollectionName = DB.CollectionName<QuickTest>(),
    };

    ObjectField objField = new ObjectField() {
        FieldName = "Qt Summary",
        BsonType = BsonType.Document,
        TypeCode = TypeCode.Object,
        Fields = [
            new CalculatedField() {
                FieldName = "Avg. Initial Power",
                BsonType = BsonType.Double,
                DefaultValue = 0.00,
                Expression = "avg([powers])",
                Variables = [
                    new CollectionPropertyVariable() {
                        Property = "Power",
                        VariableName = "powers",
                        CollectionProperty = "InitialMeasurements",
                    }
                ]
            },
            new CalculatedField() {
                FieldName = "Avg. Wl",
                BsonType = BsonType.Double,
                DefaultValue = 0.00,
                Expression = "avg([wavelengths])",
                Variables = [
                    new CollectionPropertyVariable() {
                        Property = "Wavelengths",
                        VariableName = "wavelengths",
                        CollectionProperty = "Measurements",
                    },
                ]
            }
        ]
    };

    foreach (var field in objField.Fields) {
        if (field is ObjectField oField) { } else if (field is CalculatedField cField) { } else if
            (field is ValueField vField) { } else if (field is SelectionField sField) { }
    }

    /*var script = CSharpScript.Create<double>($"QueryObject.{((CollectionVariable)pField.Variables[0]).CollectionProperty}.Select(e => e.{((CollectionVariable)pField.Variables[0]).Property}).Average();",
        globalsType:typeof(ScriptInput<QuickTest>),
        options:ScriptOptions.Default
                             .WithReferences(typeof(DB).Assembly, typeof(EpiRun).Assembly, typeof(Monitoring).Assembly)
                             .WithImports("MongoDB.Entities","MongoDB.Driver", "System.Linq"));
    script.Compile();
    var cursor = await DB.Fluent<QuickTest>().ToCursorAsync();

    while (await cursor.MoveNextAsync()) {
        foreach (var qt in cursor.Current) {
            var avg=qt.InitialMeasurements.Select(e => e.Power).Average();
            //var avg=(await script.RunAsync(new ScriptInput<QuickTest>(){QueryObject =qt})).ReturnValue;
            Console.WriteLine($"{qt.WaferId} Initial Power Avg: {avg}");
        }
    }*/
    /*var quickTests = await DB.Entity<EpiRun>().QuickTests.ChildrenFluent().ToListAsync();

    foreach (var quickTest in quickTests) {
        quickTest.InitialMeasurements.AsQueryable().Select("").Average();
    }*/
    /*var powers = quickTests.AsQueryable()
                       .SelectMany("e => e.InitialMeasurements.Select(e => e.Power)")
                       .ToListDynamic()
                       .Cast<double>();*/

    /*var script = CSharpScript.Create<double>($"queryObject.{}.AsQueryable().Select(\"\").Average() ?? 0.00",
        globalsType:typeof(ScriptInput<QuickTest>),
        options:ScriptOptions.Default
                             .WithReferences(typeof(DB).Assembly, typeof(EpiRun).Assembly, typeof(Monitoring).Assembly)
                             .WithImports("MongoDB.Entities","MongoDB.Driver", "System.Linq","System.Linq.Dynamic.Core","System.Linq.Dynamic"));*/
}

async Task GenerateEpiData() {
    var rand = new Random();
    var now = DateTime.Now;
    List<EpiRun> epiRuns = [];
    List<QuickTest> quickTests = [];
    List<XrdData> xrdMeasurementData = [];

    for (int i = 1; i <= 100; i++) {
        for (int x = 1; x <= 10; x++) {
            EpiRun run = new EpiRun() {
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

            var quickTestData = new QuickTest() {
                WaferId = run.WaferId,

                TimeStamp = now,
                InitialMeasurements = new List<QtMeasurement>() {
                    GenerateQtMeasurement(rand, "A", now),
                    GenerateQtMeasurement(rand, "B", now),
                    GenerateQtMeasurement(rand, "C", now),
                    GenerateQtMeasurement(rand, "L", now),
                    GenerateQtMeasurement(rand, "R", now),
                    GenerateQtMeasurement(rand, "T", now),
                    GenerateQtMeasurement(rand, "G", now)
                },
                FinalMeasurements = new List<QtMeasurement>() {
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
            var xrdData = new XrdData() {
                WaferId = run.WaferId,
                XrdMeasurements = new List<XrdMeasurement>() {
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

    epiRuns.ForEach(
        run => {
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
    var xrd = new XrdMeasurement() {
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
    var qt = new QtMeasurement() {
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

public class ScriptInput<TEntity> where TEntity : Entity {
    public TEntity QueryObject { get; set; }
    public string? SelectStatement { get; set; }
    public string? FilterStatement { get; set; }
}

/*var minValue = 1;
var maxValue = 10;
List<int> list = [0,2,6,4,2,4,6,10,3];

var expression = "return list.Where(x => x > minValue && x < maxValue)";
var list2a2 = Eval.Execute<List<int>>(expression, new { minValue, maxValue,list });
Console.WriteLine(string.Join(",", list2a2));*/

//await BuildDummyDatabase();

//var value=query.Select(e => e.Weight1).ToList().Average();

/*

*/


/***
 * Backup Other Scratch Work
 */

//await BuildMigration2();

/*
var run= await DB.Collection<EpiRun>().Find(_ => true).Sort(Builders<EpiRun>.Sort.Descending(e => e.ID)).FirstAsync();
Console.WriteLine("Run: "+run.WaferId);*/

//await CreateTypeConfiguration();
//TestNCalc();
//await TestDynamicScript();
//await TestBuilderFilter();
//await TestSubFilter();

/*await GenerateEpiData();
await BuildMigration();
await BuildMigration2();*/


/*await DB.DropCollectionAsync<EpiRun>();
await DB.DropCollectionAsync<QuickTest>();
await DB.DropCollectionAsync<XrdData>();
await DB.DropCollectionAsync<DocumentMigration>();
await DB.DropCollectionAsync<TypeConfiguration>();*/

/*await DB.MigrateFields();
Console.WriteLine("Check Database");*/

/*await BuildMigration2();*/

//await UndoMigration();

/***
 * Backup Scratch Work
 */

/*var list=new List<int>(){1,2,3,4,5,6,7,8,9,10};
var expression=new ExtendedExpression("where([numbers],[prop],[exp])");
expression.Parameters["numbers"]=list.Select(x=>(object?)x).ToList();
expression.Parameters["prop"]="e";
expression.Parameters["exp"]="e>5";

Console.WriteLine(JsonSerializer.Serialize(expression.Evaluate()));*/

/*//var engine = new Engine(options => options.AllowClr());
var evalContext = new EvalContext();
evalContext.RegisterAssembly(typeof(EpiRun).Assembly, typeof(Monitoring).Assembly, typeof(Monitoring).Assembly);
evalContext.RegisterUsingDirective("MongoDB.Entities");
evalContext.RegisterUsingDirective("System.Linq.Dynamic.Core");
evalContext.RegisterUsingDirective("System.Linq");
evalContext.RegisterUsingDirective("MongoDB.Driver");

string refType = "Monitoring";
string query = "Where(e=>e.EpiRun.ID==\"67c6107d81555dcdab57529c\" && e.Weight1>2).Select(e=>e.Weight2).ToList()";
string id = "67c6107d81555dcdab57529c";
string param = "Weight1";


string linq = @"
        (from e in DB.Entity<{typeName}>().Queryable()
        where e.EpiRun.ID == {id} && e.{property} > {value}
        select e.{property}).ToList()";
var linqQuery = linq.Replace("{id}", "\"67c6107d81555dcdab57529c\"").
                    Replace("{param}", param)
                   .Replace("{value}", 2.ToString()).
                    Replace("{refType}", refType);
Console.WriteLine(linqQuery);
var script = CSharpScript.Create<List<double>>(linqQuery,
    ScriptOptions.Default
                 .WithReferences(typeof(DB).Assembly, typeof(EpiRun).Assembly, typeof(Monitoring).Assembly)
                 .WithImports("MongoDB.Entities","MongoDB.Driver", "System.Linq"));*/

/*var json=JsonSerializer.Serialize(result);
Console.WriteLine(json);*/

/*Dictionary<string,object> variables = new Dictionary<string, object>(); ;

foreach(var variable in field.Variables) {
    if(variable is ReferenceVariable referenceVariable) {

    }else if (variable is ValueVariable) {

    }else if(variable is ReferenceSubVariable refSubVariable) {



        CSharpScript.Create<object>($"",
            ScriptOptions.Default.WithReferences(typeof(DB).Assembly,typeof(EpiRun).Assembly)
                         .WithImports("MongoDB.Entities","MongoDB.Driver", "System.Linq","System.Linq.Dynamic.Core"));
    }
}

var cursorAsync = await DB.Entity<EpiRun>().Fluent().Match(_ => true).ToCursorAsync();

while(await cursorAsync.MoveNextAsync()) {
    var runs = cursorAsync.Current;
    foreach(var run in runs) {

    }
}*/

/*string type="EpiRun";
string item = "W001";

DB.Entity<EpiRun>().Queryable().Where("e=>e.Name.Contains(\"W001\")").FirstOrDefault();*/

/*var result = await CSharpScript.EvaluateAsync<EpiRun>($"DB.Entity<{type}>().Fluent().Match(e=>e.Name.Contains(\"{item}\")).FirstOrDefault()",
                 ScriptOptions.Default.WithReferences(typeof(DB).Assembly,typeof(EpiRun).Assembly).WithImports("MongoDB.Entities","MongoDB.Driver"));
Console.WriteLine($"Eval Find: {result.Name}");*/

/*var result = await CSharpScript.EvaluateAsync<object>($"DB.Entity<{type}>().Queryable().Where(\"e=>e.Name.Contains(\\\"{item}\\\")\").FirstOrDefault()",
                 ScriptOptions.Default.WithReferences(typeof(DB).Assembly,typeof(EpiRun).Assembly)
                              .WithImports("MongoDB.Entities","MongoDB.Driver", "System.Linq","System.Linq.Dynamic.Core"));
Console.WriteLine($"Eval Find: {((EpiRun)result).Name}");*/

/*var script = CSharpScript.Create<object>($"DB.Entity<{type}>().Queryable().Where(\"e=>e.Name.Contains(\\\"{item}\\\")\").FirstOrDefault()",
                 ScriptOptions.Default.WithReferences(typeof(DB).Assembly,typeof(EpiRun).Assembly)
                              .WithImports("MongoDB.Entities","MongoDB.Driver", "System.Linq","System.Linq.Dynamic.Core"));*/
/*script.Compile();
Console.WriteLine(JsonSerializer.Serialize(script, new JsonSerializerOptions() { WriteIndented = true }));*/

/*var result = await script.RunAsync();
Console.WriteLine($"Eval Find: {((EpiRun)result.ReturnValue).Name}");*/

//var run=engine.Evaluate("DB.Entity<EpiRun>().Fluent().Match(e=>e.Name.Contains(\"W001\")).FirstOrDefault()");