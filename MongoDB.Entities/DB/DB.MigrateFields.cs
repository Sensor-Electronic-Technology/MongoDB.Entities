using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NCalcExtensions;

namespace MongoDB.Entities;

public static partial class DB {
#region ApplyMigration

    public static async Task ApplyMigrations() {
        var collection = Collection<EntityMigration>();
        var migrations = await collection.Find(e => !e.IsMigrated)
                                         .SortByDescending(e => e.MigrationNumber)
                                         .ToListAsync();
        Log(LogLevel.Information, "Applying migrations, Count: {Count}", migrations.Count);

        foreach (var migration in migrations) {
            switch (migration) {
                case EmbeddedMigration embMigration:
                    await ApplyMigrationsEmbedded(embMigration);
                    break;
                case DocumentMigration docMigration:
                    await ApplyDocumentMigrations(docMigration);
                    break;
            }
        }
    }

    public static async Task ApplyDocumentMigrations(DocumentMigration migration) {
        if (migration.TypeConfiguration == null) {
            Log(LogLevel.Warning, "DocumentTypeConfiguration is for migration {Migration} is null", migration.ID);

            return;
        }

        var typeConfig = await migration.TypeConfiguration.ToEntityAsync();
        Log(
            LogLevel.Information,
            "Applying migration: ID: {MigrationId} Type:{Type} Number:{Number}",
            migration.ID,
            typeConfig.TypeName,
            migration.MigrationNumber);
        var collection = Collection(typeConfig.DatabaseName, typeConfig.CollectionName);
        var cursor = await collection.FindAsync(
                         new BsonDocument(),
                         new FindOptions<BsonDocument> { BatchSize = 100 });
        List<UpdateManyModel<BsonDocument>> updates = [];

        while (await cursor.MoveNextAsync()) {
            foreach (var entity in cursor.Current) {
                //Check if AdditionalData is null, create new if null
                var doc = entity.GetElement("AdditionalData").Value.ToBsonDocument();

                if (doc.Contains("_csharpnull")) {
                    doc = [];
                }

                foreach (var op in migration.UpOperations) {
                    if (op is AddFieldOperation addField) {
                        if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == addField.Field.FieldName) ==
                            null) {
                            await AddField(addField.Field, doc, entity);
                        }
                    } else if (op is DropFieldOperation dropField) {
                        if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == dropField.Field.FieldName) !=
                            null) {
                            doc.Remove(dropField.Field.FieldName);
                        }
                    } else if (op is AlterFieldOperation alterField) {
                        if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == alterField.OldField.FieldName) !=
                            null) {
                            doc.Remove(alterField.OldField.FieldName);
                            await AddField(alterField.Field, doc, entity);
                        } else {
                            await AddField(alterField.Field, doc, entity);
                        }
                    }
                }

                var filter = Builders<BsonDocument>.Filter.Eq("_id", entity["_id"]);
                var update = Builders<BsonDocument>.Update.Set("AdditionalData", doc)
                                                   .Set("DocumentVersion", migration.Version);
                updates.Add(new(filter, update));
            }
        }

        foreach (var op in migration.UpOperations) {
            if (op is AddFieldOperation addField) {
                if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == addField.Field.FieldName) == null) {
                    typeConfig.Fields.Add(addField.Field);
                }
            } else if (op is DropFieldOperation dropField) {
                if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == dropField.Field.FieldName) != null) {
                    typeConfig.Fields.Remove(dropField.Field);
                }
            } else if (op is AlterFieldOperation alterField) {
                if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == alterField.OldField.FieldName) != null) {
                    typeConfig.Fields.Remove(alterField.OldField);
                    typeConfig.Fields.Add(alterField.Field);
                }
            }
        }

        typeConfig.DocumentVersion = migration.Version;
        typeConfig.UpdateAvailableProperties();
        await typeConfig.SaveAsync();
        migration.IsMigrated = true;
        await migration.SaveAsync();
        await collection.BulkWriteAsync(updates);
        Log(LogLevel.Information, "Migration completed");
    }

    public static async Task ApplyMigrationsEmbedded(EmbeddedMigration migration) {
        if (migration.EmbeddedTypeConfiguration == null) {
            Log(LogLevel.Warning, "EmbeddedTypeConfiguration is for migration {Migration} is null", migration.ID);
            return;
        }

        var typeConfig = await migration.EmbeddedTypeConfiguration.ToEntityAsync();
        Log(
            LogLevel.Information,
            "Applying migration: ID: {MigrationId} Type:{Type} Number:{Number}",
            migration.ID,
            typeConfig.TypeName,
            migration.MigrationNumber);

        if (!typeConfig.FieldDefinitions.TryGetValue(migration.ParentTypeName, out var fieldDef)) {
            Log(LogLevel.Error, "Failed to migrate EmbeddedMigration for {ParentCollect}", migration.ParentTypeName);
            return;
        }

        var collection = Collection(fieldDef.DatabaseName, fieldDef.ParentCollection);
        var cursor = await collection.FindAsync(new BsonDocument(), new FindOptions<BsonDocument> { BatchSize = 100 });
        List<UpdateManyModel<BsonDocument>> updates = [];

        while (await cursor.MoveNextAsync()) {
            foreach (var entity in cursor.Current) {
                var updated = false;
                foreach (var propertyName in fieldDef.PropertyNames) {
                    if (!entity.Contains(propertyName)) {
                        Log(
                            LogLevel.Warning,
                            "Property {Property} is missing from Entity: {EntityId}",
                            propertyName,
                            entity["_id"]);
                        continue;
                    }

                    if (fieldDef.IsArray) {
                        var arr = entity.GetElement(propertyName).Value.AsBsonArray;
                        if (arr.Count == 0 || arr.Contains("_csharpnull")) {
                            Log(
                                LogLevel.Warning,
                                "Array {Array} is null or empty," +
                                "No migrations applied",
                                propertyName);
                            continue;
                        }

                        foreach (var bVal in arr) {
                            await ApplyEmbeddedMigrationOperations(bVal.AsBsonDocument, migration, fieldDef);
                        }

                        entity[propertyName] = arr;
                        updated = true;
                    } else {
                        var doc = entity.GetElement(propertyName).Value.AsBsonDocument;

                        if (doc == null || doc.Contains("_csharpnull")) {
                            Log(
                                LogLevel.Warning,
                                "Embedded object {Doc} is null or empty," +
                                "No migrations applied",
                                propertyName);

                            continue;
                        }

                        await ApplyEmbeddedMigrationOperations(doc, migration, fieldDef);
                        entity[propertyName] = doc;
                        updated = true;
                    }
                }

                if (!updated) {
                    continue;
                }

                var filter = Builders<BsonDocument>.Filter.Eq("_id", entity["_id"]);
                var update = Builders<BsonDocument>.Update;
                List<UpdateDefinition<BsonDocument>> updatesList = [];

                foreach (var prop in fieldDef.PropertyNames) {
                    updatesList.Add(update.Set(e => e[prop], entity[prop]));
                }

                updates.Add(new(filter, update.Combine(updatesList)));
            }
        }

        foreach (var op in migration.UpOperations) {
            if (op is AddFieldOperation addField) {
                if (fieldDef.Fields.FirstOrDefault(e => e.FieldName == addField.Field.FieldName) == null) {
                    fieldDef.Fields.Add(addField.Field);
                }
            } else if (op is DropFieldOperation dropField) {
                if (fieldDef.Fields.FirstOrDefault(e => e.FieldName == dropField.Field.FieldName) != null) {
                    fieldDef.Fields.Remove(dropField.Field);
                }
            } else if (op is AlterFieldOperation alterField) {
                if (fieldDef.Fields.FirstOrDefault(e => e.FieldName == alterField.OldField.FieldName) != null) {
                    fieldDef.Fields.Remove(alterField.OldField);
                    fieldDef.Fields.Add(alterField.Field);
                }
            }
        }

        typeConfig.DocumentVersion = migration.Version;

        fieldDef.UpdateAvailableProperties(typeConfig.TypeName);
        await typeConfig.SaveAsync();
        migration.IsMigrated = true;
        await migration.SaveAsync();

        if (updates.Count != 0) {
            await collection.BulkWriteAsync(updates);
        }

        Log(LogLevel.Information, "Migration completed");
    }

    public static async Task RevertMigration(int migrationNumber) {
        var migration = await Collection<EntityMigration>().Find(e => e.MigrationNumber == migrationNumber)
                                                           .FirstOrDefaultAsync();

        if (migration == null) {
            return;
        }

        switch (migration) {
            case EmbeddedMigration embeddedMigration:
                await RevertEmbeddedMigration(embeddedMigration);

                break;
            case DocumentMigration docMigration:
                await RevertMigration(docMigration);

                break;
        }
    }

    public static async Task RevertMigration(DocumentMigration migration) {
        if (migration.TypeConfiguration == null) {
            Log(LogLevel.Warning, "DocumentTypeConfiguration is for migration {Migration} is null", migration.ID);

            return;
        }

        //TODO: Should you only allow the last migration to be reverted??
        /*var migrationNumber = await DB.Collection<DocumentMigration>()
                                      .Find(_ => true)
                                      .SortByDescending(e => e.MigrationNumber)
                                      .Project(e=>e.MigrationNumber)
                                      .FirstOrDefaultAsync();

        if (migration.MigrationNumber!=migrationNumber) {
            return;
        }*/
        var typeConfig = await migration.TypeConfiguration.ToEntityAsync();
        var collection = Collection(typeConfig.DatabaseName, typeConfig.CollectionName);
        var cursor = await collection.FindAsync(new BsonDocument(), new FindOptions<BsonDocument> { BatchSize = 100 });
        List<UpdateManyModel<BsonDocument>> updates = new();
        var version = migration.IsMajorVersion
                          ? typeConfig.DocumentVersion.DecrementMajor()
                          : typeConfig.DocumentVersion.Decrement();

        while (await cursor.MoveNextAsync()) {
            foreach (var entity in cursor.Current) {
                var doc = entity.GetElement("AdditionalData").Value.ToBsonDocument();

                if (doc.Contains("_csharpnull")) {
                    doc = [];
                }

                foreach (var op in migration.DownOperations) {
                    if (op is AddFieldOperation addField) {
                        if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == addField.Field.FieldName) != null) {
                            Console.WriteLine($"Failed to undo migration {migration.ID}. Field {addField.Field.FieldName} already exists");

                            continue;
                        }

                        await AddField(addField.Field, entity, doc);
                    } else if (op is DropFieldOperation dropField) {
                        if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == dropField.Field.FieldName) == null) {
                            Console.WriteLine(
                                $"Failed to drop {dropField.Field.FieldName} " +
                                $"for type {typeConfig.CollectionName} " +
                                $"in migration {migration.MigrationNumber}");

                            continue;
                        }

                        doc.Remove(dropField.Field.FieldName);
                    } else if (op is AlterFieldOperation alterField) {
                        if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == alterField.OldField.FieldName) !=
                            null) {
                            doc.Remove(alterField.OldField.FieldName);
                            await AddField(alterField.Field, doc, entity);
                        } else {
                            await AddField(alterField.Field, doc, entity);
                        }
                    }

                    //Add update to queue
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", entity["_id"]);
                    var update = Builders<BsonDocument>.Update.Set("AdditionalData", doc)
                                                       .Set("DocumentVersion", version);
                    updates.Add(new(filter, update));
                }
            }
        } //End Entity Updates

        foreach (var op in migration.DownOperations) {
            if (op is AddFieldOperation addField) {
                if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == addField.Field.FieldName) == null) {
                    typeConfig.Fields.Add(addField.Field);
                }
            } else if (op is DropFieldOperation dropField) {
                if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == dropField.Field.FieldName) != null) {
                    typeConfig.Fields.Remove(dropField.Field);
                }
            } else if (op is AlterFieldOperation alterField) {
                if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == alterField.OldField.FieldName) != null) {
                    typeConfig.Fields.Remove(alterField.OldField);
                    typeConfig.Fields.Add(alterField.Field);
                }
            }
        } //End DocumentTypeConfiguration Update

        //Update documents,Update DocumentTypeConfiguration, Delete migration
        typeConfig.DocumentVersion = version;
        await typeConfig.SaveAsync();
        await collection.BulkWriteAsync(updates);
        await migration.DeleteAsync();
    }

    public static async Task RevertEmbeddedMigration(EmbeddedMigration migration) {
        if (migration.EmbeddedTypeConfiguration == null) {
            Log(LogLevel.Warning, "EmbeddedTypeConfiguration is null for migration {Migration} is null", migration.ID);
            return;
        }

        var typeConfig = await migration.EmbeddedTypeConfiguration.ToEntityAsync();

        if (!typeConfig.FieldDefinitions.TryGetValue(migration.ParentTypeName, out var fieldDef)) {
            Log(LogLevel.Error, "FieldDefinitions for ParentType {Parent} are missing", migration.ParentTypeName);
            return;
        }

        var collection = Collection(fieldDef.DatabaseName, fieldDef.ParentCollection);
        var cursor = await collection.FindAsync(new BsonDocument(), new FindOptions<BsonDocument> { BatchSize = 100 });
        List<UpdateManyModel<BsonDocument>> updates = new();
        var version = migration.IsMajorVersion
                          ? typeConfig.DocumentVersion.DecrementMajor()
                          : typeConfig.DocumentVersion.Decrement();

        while (await cursor.MoveNextAsync()) {
            foreach (var entity in cursor.Current) {
                bool updated = false;

                foreach (var propertyName in fieldDef.PropertyNames) {
                    if (!entity.Contains(propertyName)) {
                        Log(
                            LogLevel.Warning,
                            "Property {Property} is missing from Entity: {EntityId}",
                            propertyName,
                            entity["_id"]);

                        continue;
                    }

                    if (fieldDef.IsArray) {
                        var arr = entity.GetElement(propertyName).Value.AsBsonArray;

                        if (arr.Count == 0 || arr.Contains("_csharpnull")) {
                            Log(
                                LogLevel.Information,
                                "Array {Array} is null or empty, no migrations reverted",
                                propertyName);
                            continue;
                        }

                        foreach (var bVal in arr) {
                            await RevertOperations(bVal.AsBsonDocument, migration, fieldDef);
                        }

                        entity[propertyName] = arr;
                        updated = true;
                    } else {
                        var doc = entity.GetElement(propertyName).Value.AsBsonDocument;
                        if (doc == null || doc.Contains("_csharpnull")) {
                            Log(
                                LogLevel.Information,
                                "Embedded object {Doc} is null or empty," +
                                "No migrations reverted",
                                propertyName);
                            continue;
                        }

                        await RevertOperations(doc, migration, fieldDef);
                        entity[propertyName] = doc;
                        updated = true;
                    }

                    if (!updated) {
                        continue;
                    }

                    var filter = Builders<BsonDocument>.Filter.Eq("_id", entity["_id"]);
                    var update = Builders<BsonDocument>.Update;
                    List<UpdateDefinition<BsonDocument>> updatesList = [];

                    foreach (var prop in fieldDef.PropertyNames) {
                        updatesList.Add(update.Set(e => e[prop], entity[prop]));
                    }

                    updates.Add(new(filter, update.Combine(updatesList)));
                }
            }
        } //End Entity Updates

        foreach (var op in migration.DownOperations) {
            if (op is AddFieldOperation addField) {
                if (fieldDef.Fields.FirstOrDefault(e => e.FieldName == addField.Field.FieldName) == null) {
                    fieldDef.Fields.Add(addField.Field);
                }
            } else if (op is DropFieldOperation dropField) {
                if (fieldDef.Fields.FirstOrDefault(e => e.FieldName == dropField.Field.FieldName) != null) {
                    fieldDef.Fields.Remove(dropField.Field);
                }
            } else if (op is AlterFieldOperation alterField) {
                if (fieldDef.Fields.FirstOrDefault(e => e.FieldName == alterField.OldField.FieldName) != null) {
                    fieldDef.Fields.Remove(alterField.OldField);
                    fieldDef.Fields.Add(alterField.Field);
                }
            }
        } //End DocumentTypeConfiguration Update

        typeConfig.DocumentVersion = version;

        if (fieldDef.Fields.Count == 0) {
            typeConfig.FieldDefinitions.Remove(migration.ParentTypeName);
        } else {
            typeConfig.FieldDefinitions[migration.ParentTypeName]
                      .UpdateAvailableProperties(migration.ParentTypeName);
        }

        await typeConfig.SaveAsync();

        if (updates.Any()) {
            await collection.BulkWriteAsync(updates);
        }

        await migration.DeleteAsync();
    }

#endregion

#region FieldsAndOperations

    internal static async Task AddField(Field field, BsonDocument doc, BsonDocument entity) {
        switch (field) {
            case ObjectField oField: {
                BsonDocument objDoc = !doc.Contains(oField.FieldName) ? new BsonDocument() : doc[oField.FieldName].AsBsonDocument;
                foreach (var f in oField.Fields) {
                    await AddField(f, objDoc, entity);
                }

                doc[oField.FieldName] = objDoc;
                break;
            }
            case ValueField vField: {
                if (vField is CalculatedField calcField) {
                    var expression = await ProcessCalculationField(calcField, doc, entity);
                    if (calcField.IsBooleanExpression) {
                        object result = ((bool)expression.Evaluate()) ? calcField.TrueValue : calcField.FalseValue;
                        doc[calcField.FieldName] = BsonValue.Create(result);
                    } else {
                        doc[calcField.FieldName] = BsonValue.Create(expression.Evaluate());
                    }
                } else {
                    if (!doc.Contains(vField.FieldName)) {
                        doc[vField.FieldName] = BsonValue.Create(vField.DefaultValue);
                    }
                }

                break;
            }
            case SelectionField sField: {
                if (!doc.Contains(sField.FieldName)) {
                    doc[sField.FieldName] = BsonValue.Create(sField.DefaultValue);
                }

                break;
            }
        }
    }

    internal static async Task UpdateField(Field field, Field oldField, BsonDocument doc, BsonDocument entity) {
        if (field is ObjectField oField) {
            var objDoc = new BsonDocument();

            foreach (var f in oField.Fields) {
                await AddField(f, objDoc, entity);
            }

            doc[oField.FieldName] = objDoc;
        } else if (field is ValueField vField) {
            doc.Add(vField.FieldName, BsonValue.Create(vField.DefaultValue));
        } else if (field is SelectionField sField) {
            doc.Add(sField.FieldName, BsonValue.Create(sField.DefaultValue));
        } else if (field is CalculatedField cField) {
            var expression = await ProcessCalculationField(cField, doc, entity);
            doc.Add(cField.FieldName, BsonValue.Create(expression.Evaluate()));
        }
    }

    internal static async Task<ExtendedExpression> ProcessCalculationField(CalculatedField cField,
                                                                           BsonDocument doc,
                                                                           BsonDocument entity) {
        var expression = new ExtendedExpression(cField.Expression);

        foreach (var variable in cField.Variables) {
            if (variable is ValueVariable vVar) {
                expression.Parameters[vVar.VariableName] = vVar.Value;
            } else if (variable is PropertyVariable pVar) {
                if (pVar is EmbeddedPropertyVariable embeddedVar) {
                    var emDoc = entity[embeddedVar.Property].AsBsonDocument;

                    for (int i = 0; i < embeddedVar.EmbeddedObjectProperties.Count; i++) {
                        emDoc = emDoc[embeddedVar.EmbeddedObjectProperties[i]].AsBsonDocument;
                    }

                    expression.Parameters[embeddedVar.VariableName] = embeddedVar.DataType switch {
                        DataType.NUMBER => emDoc[embeddedVar.EmbeddedProperty].AsDouble,
                        DataType.STRING => emDoc[embeddedVar.EmbeddedProperty].AsString ?? "",
                        DataType.BOOLEAN => emDoc[embeddedVar.EmbeddedProperty].AsBoolean,
                        DataType.DATE => DateTime.Parse(emDoc[embeddedVar.EmbeddedProperty].AsString),
                        DataType.LIST_NUMBER => emDoc[embeddedVar.EmbeddedProperty].AsBsonArray.Select(e => e.AsDouble),
                        DataType.LIST_STRING => emDoc[embeddedVar.EmbeddedProperty].AsBsonArray.Select(e => e.AsString),
                        DataType.LIST_BOOLEAN => emDoc[embeddedVar.EmbeddedProperty].AsBsonArray
                                                                                    .Select(e => e.AsBoolean),
                        DataType.LIST_DATE => emDoc[embeddedVar.EmbeddedProperty].AsBsonArray
                                                                                 .Select(e => DateTime.Parse(e.AsString)),
                        _ => emDoc[embeddedVar.EmbeddedProperty].AsDouble
                    };
                } else if (pVar is CollectionPropertyVariable cVar) {
                    if (entity.Contains(cVar.CollectionProperty)) {
                        IQueryable<BsonValue>? query;
                        Console.WriteLine(cVar.Filter?.ToString() ?? "Empty Filter");

                        if (cVar.Filter != null) {
                            query = entity[cVar.CollectionProperty].AsBsonArray.AsQueryable()
                                                                   .Where(cVar.Filter.ToString());
                        } else {
                            query = entity[cVar.CollectionProperty].AsBsonArray.AsQueryable();
                        }

                        if (query.Count() != 0) {
                            expression.Parameters[cVar.VariableName] = cVar.DataType switch {
                                DataType.NUMBER => query.Select($"e=>e.{cVar.Property}.AsDouble").FirstOrDefault(),
                                DataType.STRING => query.Select($"e=>e.{cVar.Property}.AsString").FirstOrDefault() ??
                                                   "",
                                DataType.BOOLEAN => query.Select($"e=>e.{cVar.Property}.AsBoolean").FirstOrDefault(),
                                DataType.DATE => query.Select(e => DateTime.Parse(e[cVar.Property].AsString))
                                                      .FirstOrDefault(),
                                DataType.LIST_NUMBER => query.Select(e => e[cVar.Property].AsDouble),
                                DataType.LIST_STRING => query.Select(e => e[cVar.Property].AsString),
                                DataType.LIST_BOOLEAN => query.Select(e => e[cVar.Property].AsBoolean),
                                DataType.LIST_DATE => query.Select(e => DateTime.Parse(e[cVar.Property].AsString)),
                                _ => query.Select(e => e[cVar.Property].AsDouble)
                            };
                        } else {
                            expression.Parameters[cVar.VariableName] = cVar.DataType switch {
                                DataType.NUMBER => 0.00,
                                DataType.STRING => "",
                                DataType.BOOLEAN => false,
                                DataType.DATE => DateTime.MinValue,
                                DataType.LIST_NUMBER => new List<double> { 0, 0, 0 },
                                DataType.LIST_STRING => new List<string> { "", "", "" },
                                DataType.LIST_BOOLEAN => new List<bool> { false, false, false },
                                DataType.LIST_DATE => new List<DateTime>
                                    { DateTime.MinValue, DateTime.MinValue, DateTime.MinValue },
                                _ => throw new ArgumentException("Empty Value type not supported")
                            };
                        }
                    }
                } else if (pVar is RefPropertyVariable rVar) {
                    var collection = Collection(rVar.DatabaseName, rVar.CollectionName).AsQueryable();

                    if (rVar.Filter != null) {
                        collection.Where(rVar.Filter.ToString());
                    }

                    if (collection.Any()) {
                        expression.Parameters[rVar.VariableName] = rVar.DataType switch {
                            DataType.NUMBER => collection.Select(e => e[rVar.Property].AsDouble).FirstOrDefault(),
                            DataType.STRING => collection.Select(e => e[rVar.Property].AsString).FirstOrDefault(),
                            DataType.BOOLEAN => collection.Select(e => e[rVar.Property].AsBoolean).FirstOrDefault(),
                            DataType.DATE => collection.Select(e => DateTime.Parse(e[rVar.Property].AsString))
                                                       .FirstOrDefault(),
                            DataType.LIST_NUMBER => collection.Select(e => e[rVar.Property].AsDouble),
                            DataType.LIST_STRING => collection.Select(e => e[rVar.Property].AsString),
                            DataType.LIST_BOOLEAN => collection.Select(e => e[rVar.Property].AsBoolean),
                            DataType.LIST_DATE => collection.Select(e => DateTime.Parse(e[rVar.Property].AsString)),
                            _ => throw new ArgumentException("Empty Value type not supported")
                        };
                    } else {
                        expression.Parameters[rVar.VariableName] = rVar.DataType switch {
                            DataType.NUMBER => 0.00,
                            DataType.STRING => "",
                            DataType.BOOLEAN => false,
                            DataType.DATE => DateTime.MinValue,
                            DataType.LIST_NUMBER => new List<double>(),
                            DataType.LIST_STRING => new List<string>(),
                            DataType.LIST_BOOLEAN => new List<bool>(),
                            DataType.LIST_DATE => new List<DateTime>(),
                            _ => throw new ArgumentException("Empty Value type not supported")
                        };
                    }
                } else if (pVar is RefCollectionPropertyVariable rcVar) {
                    var collection = Collection(rcVar.DatabaseName, rcVar.CollectionName);
                    List<BsonDocument> list = [];

                    if (rcVar.Filter != null) {
                        list = await collection.AsQueryable().Where(rcVar.Filter.ToString()).ToListAsync();
                    } else {
                        list = await collection.Find(_ => true).ToListAsync();
                    }

                    var query = list.SelectMany(e => e[rcVar.CollectionProperty].AsBsonArray).ToList();

                    if (rcVar.SubFilter != null) {
                        query.AsQueryable().Where(rcVar.SubFilter.ToString());
                    }

                    if (query.Count != 0) {
                        expression.Parameters[rcVar.VariableName] = rcVar.DataType switch {
                            DataType.NUMBER => query.Select(e => e[rcVar.Property].AsDouble).FirstOrDefault(),
                            DataType.STRING => query.Select(e => e[rcVar.Property].AsString).FirstOrDefault(),
                            DataType.BOOLEAN => query.Select(e => e[rcVar.Property].AsBoolean).FirstOrDefault(),
                            DataType.DATE => query.Select(e => DateTime.Parse(e[rcVar.Property].AsString))
                                                  .FirstOrDefault(),
                            DataType.LIST_NUMBER => query.Select(e => e[rcVar.Property].AsDouble),
                            DataType.LIST_STRING => query.Select(e => e[rcVar.Property].AsString),
                            DataType.LIST_BOOLEAN => query.Select(e => e[rcVar.Property].AsBoolean),
                            DataType.LIST_DATE => query.Select(e => DateTime.Parse(e[rcVar.Property].AsString)),
                            _ => throw new ArgumentException("Empty Value type not supported")
                        };
                    } else {
                        expression.Parameters[rcVar.VariableName] = rcVar.DataType switch {
                            DataType.NUMBER => 0.00,
                            DataType.STRING => "",
                            DataType.BOOLEAN => false,
                            DataType.DATE => DateTime.MinValue,
                            DataType.LIST_NUMBER => new List<double>(),
                            DataType.LIST_STRING => new List<string>(),
                            DataType.LIST_BOOLEAN => new List<bool>(),
                            DataType.LIST_DATE => new List<DateTime>(),
                            _ => throw new ArgumentException("Empty Value type not supported")
                        };
                    }
                } else {
                    if (entity.Contains(pVar.Property)) {
                        expression.Parameters[pVar.VariableName] = entity[pVar.Property];
                    }
                }
            }
        }

        return expression;
    }

    internal static async Task RevertOperations(BsonDocument embeddedEntity,
                                                EmbeddedMigration migration,
                                                EmbeddedFieldDefinitions fieldDef) {
        var doc = embeddedEntity.GetElement("AdditionalData").Value.ToBsonDocument();

        if (doc.Contains("_csharpnull")) {
            doc = new BsonDocument();
        }

        foreach (var op in migration.DownOperations) {
            switch (op) {
                case AddFieldOperation addField: {
                    if (fieldDef.Fields.FirstOrDefault(e => e.FieldName == addField.Field.FieldName) != null) {
                        Console.WriteLine($"Failed to undo migration {migration.ID}. Field {addField.Field.FieldName} already exists");

                        continue;
                    }

                    await AddField(addField.Field, doc, embeddedEntity);

                    break;
                }
                case DropFieldOperation dropField: {
                    if (fieldDef.Fields.FirstOrDefault(e => e.FieldName == dropField.Field.FieldName) == null) {
                        Console.WriteLine(
                            $"Failed to drop {dropField.Field.FieldName} " +
                            $"for type {fieldDef.ParentCollection} " +
                            $"in migration {migration.MigrationNumber}");

                        continue;
                    }

                    doc.Remove(dropField.Field.FieldName);

                    break;
                }
                case AlterFieldOperation alterField: {
                    if (fieldDef.Fields.FirstOrDefault(e => e.FieldName == alterField.OldField.FieldName) !=
                        null) {
                        doc.Remove(alterField.OldField.FieldName);
                    }

                    await AddField(alterField.Field, doc, embeddedEntity);

                    break;
                }
            }
        }
    }

    internal static async Task ApplyEmbeddedMigrationOperations(BsonDocument embeddedDoc,
                                                                EmbeddedMigration migration,
                                                                EmbeddedFieldDefinitions fieldDef) {
        var addDataDoc = embeddedDoc.GetElement("AdditionalData").Value.ToBsonDocument();

        if (addDataDoc == null || addDataDoc.Contains("_csharpnull")) {
            addDataDoc = [];
        }

        foreach (var op in migration.UpOperations) {
            if (op is AddFieldOperation addField) {
                if (fieldDef.Fields.FirstOrDefault(e => e.FieldName == addField.Field.FieldName) ==
                    null) {
                    await AddField(addField.Field, addDataDoc, embeddedDoc);
                }
            } else if (op is DropFieldOperation dropField) {
                if (fieldDef.Fields.FirstOrDefault(e => e.FieldName == dropField.Field.FieldName) !=
                    null) {
                    addDataDoc.Remove(dropField.Field.FieldName);
                }
            } else if (op is AlterFieldOperation alterField) {
                if (fieldDef.Fields.FirstOrDefault(e => e.FieldName == alterField.OldField.FieldName) !=
                    null) {
                    addDataDoc.Remove(alterField.OldField.FieldName);
                }

                await AddField(alterField.Field, addDataDoc, embeddedDoc);
            }
        }

        embeddedDoc["AdditionalData"] = addDataDoc;
    }

#endregion
}