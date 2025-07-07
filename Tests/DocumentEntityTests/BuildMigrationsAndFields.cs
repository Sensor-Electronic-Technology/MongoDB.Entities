using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[TestClass]
public class Fields {
    private async Task InitQuickTest() {
        await InitTest.InitTestDatabase(assemblies: typeof(EpiRun).Assembly);
        await DB.DropCollectionAsync<EpiRun>();
        await DB.DropCollectionAsync<QuickTest>();
        await DB.DropCollectionAsync<XrdData>();
        await DB.DropCollectionAsync<DocumentMigration>();
        await DB.DropCollectionAsync<DocumentTypeConfiguration>();
        await DataHelper.GenerateEpiData();
        var migrationNumber = await DB.Collection<DocumentMigration>()
                                      .Find(_ => true)
                                      .SortByDescending(e => e.MigrationNumber)
                                      .Project(e => e.MigrationNumber)
                                      .FirstOrDefaultAsync();
        MigrationBuilder builder = new MigrationBuilder();
        ObjectField objField = new ObjectField {
            FieldName = "Qt Summary",
            BsonType = BsonType.Document,
            TypeCode = TypeCode.Object,
            Fields = [
                new CalculatedField {
                    FieldName = "Avg. Initial Power",
                    BsonType = BsonType.Double,
                    TypeCode = TypeCode.Double,
                    DataType = DataType.NUMBER,
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
                    DataType = DataType.NUMBER,
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
        var typeConfig = DocumentTypeConfiguration.CreateOnline<QuickTest>();
        Assert.IsNotNull(typeConfig);
        await typeConfig.SaveAsync();
        var migration = builder.Build(typeConfig, migrationNumber);
        await migration.SaveAsync();
        await typeConfig.Migrations.AddAsync(migration);
    }
    [TestMethod]
    public async Task test_migrate_build_object_field() {
        await InitQuickTest();
        await Task.Delay(500);
        var internalTypeConfig = DB.TypeConfiguration<QuickTest>();
        Assert.IsNotNull(internalTypeConfig);
        var typeConfig=await DB.Collection<DocumentTypeConfiguration>().Find(e=>e.ID==internalTypeConfig.ID).FirstOrDefaultAsync();
        Assert.IsNotNull(typeConfig);
        Assert.IsNotEmpty(typeConfig.Migrations);
        
        var migration=DB.Collection<DocumentMigration>()
                        .Find(e=>e.TypeConfiguration!=null && e.TypeConfiguration.ID==typeConfig.ID)
                        .FirstOrDefaultAsync();
        Assert.IsNotNull(migration);
        //Check for last migration
    }

    [TestMethod]
    public async Task test_migrate_object_field() {
        await InitTest.InitTestDatabase(assemblies: typeof(EpiRun).Assembly);
        await DB.ApplyMigrations();
        var typeConfig = DB.TypeConfiguration<QuickTest>();
        Assert.IsNotNull(typeConfig);
        Assert.IsNotEmpty(typeConfig.Fields);
        var collection = DB.Collection(typeConfig.DatabaseName,typeConfig.CollectionName);
        var cursor = await collection.FindAsync(new BsonDocument(), new FindOptions<BsonDocument> {BatchSize = 10});
        var field = typeConfig.Fields[0];
        Assert.IsTrue(field is ObjectField);
        var objField=field as ObjectField;
        Assert.IsNotNull(objField);
        while (await cursor.MoveNextAsync()) {
            foreach (var entity in cursor.Current) {
                var doc=entity.GetElement("AdditionalData").Value.ToBsonDocument();
                Assert.IsFalse(doc.Contains("_csharpnull"));
                Assert.IsTrue(doc.Contains(field.FieldName));
                var objDoc=doc[field.FieldName].AsBsonDocument;
                Assert.IsNotNull(objDoc);
                foreach (var subField in objField.Fields) {
                    Assert.IsTrue(objDoc.Contains(subField.FieldName));
                }
            }
        }
    }

    [TestMethod]
    public async Task test_migrate_selection_field() {
        ObjectField objField = new ObjectField {
            FieldName = "Qt Summary",
            BsonType = BsonType.Document,
            TypeCode = TypeCode.Object,
            Fields = [
                new SelectionField() {
                    FieldName = "PassFail",
                    BsonType = BsonType.String,
                    DefaultValue = "Pass",
                    SelectionDictionary = new() {
                        { "Pass", "Value1" },
                        { "Fail", "Value2" }
                    },
                    TypeCode = TypeCode.String
                },
                new SelectionField() {
                    FieldName = "AvailableOptions",
                    BsonType = BsonType.Double,
                    DefaultValue = 0.00,
                    TypeCode = TypeCode.Double,
                    SelectionDictionary = new() {
                        { "Option 1", 0.00 },
                        { "Option 2", 1.50},
                        { "Option 3", 2.54},
                        { "Option 4", 3.56},
                        { "Option 5", 7.65},
                    }
                },
                new SelectionField() {
                    FieldName = "PersonInCharge",
                    BsonType = BsonType.Array,
                    DefaultValue = "PersonInCharge",
                    SelectionDictionary = new() {
                        { "Andrew", "Andrew Elmendorf,aelmendorf@s-et.com" },
                        { "Rakesh", "Rakesh Jain,rjain@s-et.com"},
                        { "Graci", "Graci Hill,ghill@s-et.com"},
                    }
                }
            ]
        };

        
    }

    [ClassCleanup]
    public static async Task Cleanup() {
        await DB.DropCollectionAsync<EpiRun>();
        await DB.DropCollectionAsync<QuickTest>();
        await DB.DropCollectionAsync<XrdData>();
        await DB.DropCollectionAsync<DocumentMigration>();
        await DB.DropCollectionAsync<DocumentTypeConfiguration>();
    }
}