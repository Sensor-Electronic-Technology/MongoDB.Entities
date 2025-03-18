using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NCalcExtensions;

namespace MongoDB.Entities;

public static partial class DB {
    public static async Task MigrateFields() {
        var migrateCollection = Collection<DocumentMigration>();
        var migrations = await migrateCollection.Find(e => !e.IsMigrated)
                                                .SortByDescending(e => e.MigrationNumber)
                                                .ToListAsync();
        foreach (var migration in migrations) {
            if (migration.TypeConfiguration == null) {
                //TODO: Add logger
                Console.WriteLine("TypeConfiguration is null");
                return;
            }
            var typeConfig = await migration.TypeConfiguration.ToEntityAsync();
            var collection = DB.Collection(typeConfig.DatabaseName,typeConfig.CollectionName);
            var cursor = await collection.FindAsync(new BsonDocument(), new FindOptions<BsonDocument>(){BatchSize = 100});
            List<UpdateManyModel<BsonDocument>> updates = [];
            while (await cursor.MoveNextAsync()) {
                foreach (var entity in cursor.Current) {
                    //Check if AdditionalData is null, create new if null
                    var doc=entity.GetElement("AdditionalData").Value.ToBsonDocument();
                    if (doc.Contains("_csharpnull")) {
                        doc = new BsonDocument();
                    }
                    foreach (var op in migration.UpOperations) {
                        if (op is AddFieldOperation addField) {
                            if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == addField.Field.FieldName) == null) {
                                await AddField(typeConfig,addField.Field,doc,entity);
                            }
                        }else if (op is DropFieldOperation dropField) {
                            if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == dropField.Field.FieldName) != null) {
                                doc.Remove(dropField.Field.FieldName);
                            }
                        }else if (op is AlterFieldOperation alterField) {
                            if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == alterField.OldField.FieldName) != null) {
                                doc.Remove(alterField.OldField.FieldName);
                                await AddField(typeConfig,alterField.Field,doc,entity);
                            } else {
                                await AddField(typeConfig,alterField.Field,doc,entity);
                            }
                        }
                    }
                    //Add update to queue
                    var filter=Builders<BsonDocument>.Filter.Eq("_id",entity["_id"]);
                    var update=Builders<BsonDocument>.Update.Set("AdditionalData",doc)
                                                     .Set("DocumentVersion",migration.Version);
                    updates.Add(new(filter,update));
                }
                
            }
            foreach (var op in migration.UpOperations) {
                if (op is AddFieldOperation addField) {
                    if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == addField.Field.FieldName) == null) {
                        typeConfig.Fields.Add(addField.Field);
                    }
                }else if (op is DropFieldOperation dropField) {
                    if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == dropField.Field.FieldName) != null) {
                        typeConfig.Fields.Remove(dropField.Field);
                    }
                }else if (op is AlterFieldOperation alterField) {
                    if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == alterField.OldField.FieldName)!=null) {
                        typeConfig.Fields.Remove(alterField.OldField);
                        typeConfig.Fields.Add(alterField.Field);
                    }
                }
            }
            typeConfig.DocumentVersion = migration.Version;
            await typeConfig.SaveAsync();
            migration.IsMigrated = true;
            await migration.SaveAsync();
            await collection.BulkWriteAsync(updates);
        }
    }
    public static async Task UndoMigration(DocumentMigration migration) {
        //var logger = CreateLogger;
        if (migration.TypeConfiguration == null) {
            //logger.LogError("Failed to undo migration {MigrationId}. TypeConfiguration is null",migration.ID);
            Console.WriteLine($"Failed to undo migration {migration.ID}, TypeConfiguration is null");
            return;
        }
        var typeConfig = await migration.TypeConfiguration.ToEntityAsync();
        var collection = DB.Collection(typeConfig.DatabaseName, typeConfig.CollectionName);
        var cursor=await collection.FindAsync(new BsonDocument(),new FindOptions<BsonDocument>(){BatchSize=100});
        List<UpdateManyModel<BsonDocument>> updates = new();
        while (await cursor.MoveNextAsync()) {
            foreach (var entity in cursor.Current) {
                var doc=entity.GetElement("AdditionalData").Value.ToBsonDocument();
                if (doc.Contains("_csharpnull")) {
                    doc = new BsonDocument();
                }
                foreach (var op in migration.DownOperations) {
                    if (op is AddFieldOperation addField) {
                        if(typeConfig.Fields.FirstOrDefault(e=>e.FieldName==addField.Field.FieldName)!=null){
                            /*logger.LogError("Failed to undo migration {MigrationId} operation. " +
                                            "Field {FieldName} already exists",
                                migration.ID,addField.Field.FieldName);*/
                            Console.WriteLine($"Failed to undo migration {migration.ID}. Field {addField.Field.FieldName} already exists");
                            continue;
                        }
                        await AddField(typeConfig,addField.Field,entity,doc);
                    }else if (op is DropFieldOperation dropField) {
                        if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == dropField.Field.FieldName)==null) {
                            Console.WriteLine($"Failed to drop {dropField.Field.FieldName} " +
                                              $"for type {typeConfig.CollectionName} " +
                                              $"in migration {migration.MigrationNumber}");
                            continue;
                        }
                        doc.Remove(dropField.Field.FieldName);
                    }else if (op is AlterFieldOperation alterField) {
                        if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == alterField.OldField.FieldName) != null) {
                            doc.Remove(alterField.OldField.FieldName);
                            await AddField(typeConfig,alterField.Field,doc,entity);
                        } else {
                            await AddField(typeConfig,alterField.Field,doc,entity);
                        }
                    }
                    //Add update to queue
                    var filter=Builders<BsonDocument>.Filter.Eq("_id",entity["_id"]);
                    var update=Builders<BsonDocument>.Update.Set("AdditionalData",doc);
                    updates.Add(new(filter,update));
                }
            }
        }//End Entity Updates
        foreach (var op in migration.DownOperations) {
            if (op is AddFieldOperation addField) {
                if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == addField.Field.FieldName) == null) {
                    typeConfig.Fields.Add(addField.Field);
                }
            }else if (op is DropFieldOperation dropField) {
                if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == dropField.Field.FieldName) != null) {
                    typeConfig.Fields.Remove(dropField.Field);
                }
            }else if (op is AlterFieldOperation alterField) {
                if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == alterField.OldField.FieldName)!=null) {
                    typeConfig.Fields.Remove(alterField.OldField);
                    typeConfig.Fields.Add(alterField.Field);
                }
            }
        }//End TypeConfiguration Update
        
        //Update documents,Update TypeConfiguration, Delete migration
        await typeConfig.SaveAsync();
        await collection.BulkWriteAsync(updates);
        await migration.DeleteAsync();
    }
    public static async Task MigrateEntity<TEntity>(TEntity entity,CancellationToken cancellation = default) where TEntity : DocumentEntity {
        var typeConfig = DB.TypeConfiguration<TEntity>() ?? await Find<TypeConfiguration>()
                                                                  .Match(e => e.CollectionName == CollectionName<TEntity>())
                                                                  .ExecuteFirstAsync(cancellation);
        if (typeConfig == null) {
            return;
        }
        
        if (!typeConfig.Migrations.Any()) {
            return;
        }
        var migrations=await typeConfig.Migrations.ChildrenQueryable().ToListAsync(cancellationToken: cancellation);

        if (migrations.Count <= 0) {
            return;
        }
        
        if (entity.AdditionalData == null) {
            entity.AdditionalData= [];
        }
        var doc = entity.AdditionalData;
        var entityDoc=entity.ToBsonDocument();
        
        foreach (var migration in migrations) {
            foreach (var op in migration.UpOperations) {
                if (op is AddFieldOperation addField) {
                    await AddField(typeConfig,addField.Field,doc,entityDoc);
                }else if (op is DropFieldOperation dropField) {
                    doc.Remove(dropField.Field.FieldName);
                }else if (op is AlterFieldOperation alterField) {
                    doc.Remove(alterField.OldField.FieldName);
                    await AddField(typeConfig,alterField.Field,doc,entityDoc);
                }
            }
        }
    }
    public static async Task AddField(TypeConfiguration typeConfig, Field field, BsonDocument doc,BsonDocument entity) {
        if (field is ObjectField oField) {
            var objDoc=new BsonDocument();
            foreach(var f in oField.Fields) { 
                await AddField(typeConfig,f,objDoc,entity);
            }
            doc[oField.FieldName] = objDoc;
        }else if (field is ValueField vField) {
            if (vField is CalculatedField calcField) {
                var expression=await ProcessCalculationField(calcField,doc,entity);
                if (calcField.IsBooleanExpression) {
                    object result=((bool)expression.Evaluate()) ? calcField.TrueValue : calcField.FalseValue;
                    doc[calcField.FieldName] = BsonValue.Create(result);
                } else {
                    doc[calcField.FieldName] = BsonValue.Create(expression.Evaluate());
                }
            }else {
                doc[vField.FieldName] = BsonValue.Create(vField.DefaultValue);
            }
        }else if (field is SelectionField sField) {
            doc[sField.FieldName] = BsonValue.Create(sField.DefaultValue);
        }
    }
    public static async Task UpdateField(TypeConfiguration typeConfig, Field field,Field oldField, BsonDocument doc,BsonDocument entity) {
        if (field is ObjectField oField) {
            var objDoc=new BsonDocument();
            foreach(var f in oField.Fields) { 
                await AddField(typeConfig,f,objDoc,entity);
            }
            doc[oField.FieldName] = objDoc;
        }else if (field is ValueField vField) {
            doc.Add(vField.FieldName,BsonValue.Create(vField.DefaultValue));
        }else if (field is SelectionField sField) {
            doc.Add(sField.FieldName,BsonValue.Create(sField.DefaultValue));
        }else if (field is CalculatedField cField) {
            var expression=await ProcessCalculationField(cField,doc,entity);
            doc.Add(cField.FieldName,BsonValue.Create(expression.Evaluate()));
        }
    }
    public static async Task<ExtendedExpression> ProcessCalculationField(CalculatedField cField, BsonDocument doc, BsonDocument entity) {
        var expression = new ExtendedExpression(cField.Expression);
        foreach (var variable in cField.Variables) {
            if (variable is ValueVariable vVar) {
                expression.Parameters[vVar.VariableName] = vVar.Value;
            }else if (variable is PropertyVariable pVar) {
                if (pVar is EmbeddedPropertyVariable embeddedVar) {
                    var emDoc = entity[embeddedVar.Property].AsBsonDocument;
                    for (int i = 0; i < embeddedVar.EmbeddedObjectProperties.Count; i++) {
                        emDoc=emDoc[embeddedVar.EmbeddedObjectProperties[i]].AsBsonDocument;
                    }
                    expression.Parameters[embeddedVar.VariableName] = embeddedVar.VariableType switch {
                        VariableType.NUMBER => emDoc[embeddedVar.EmbeddedProperty].AsDouble,
                        VariableType.STRING => emDoc[embeddedVar.EmbeddedProperty].AsString ?? "",
                        VariableType.BOOLEAN => emDoc[embeddedVar.EmbeddedProperty].AsBoolean,
                        VariableType.DATE => DateTime.Parse(emDoc[embeddedVar.EmbeddedProperty].AsString),
                        VariableType.LIST_NUMBER => emDoc[embeddedVar.EmbeddedProperty].AsBsonArray.Select(e => e.AsDouble),
                        VariableType.LIST_STRING => emDoc[embeddedVar.EmbeddedProperty].AsBsonArray.Select(e => e.AsString),
                        VariableType.LIST_BOOLEAN => emDoc[embeddedVar.EmbeddedProperty].AsBsonArray.Select(e => e.AsBoolean),
                        VariableType.LIST_DATE => emDoc[embeddedVar.EmbeddedProperty].AsBsonArray.Select(e => DateTime.Parse(e.AsString)),
                        _ => emDoc[embeddedVar.EmbeddedProperty].AsDouble
                    };
                }else if (pVar is CollectionPropertyVariable cVar) { 
                    if (entity.Contains(cVar.CollectionProperty)) {
                        IQueryable<BsonValue>? query;
                        Console.WriteLine(cVar.Filter?.ToString() ?? "Empty Filter");
                        if (cVar.Filter != null) {
                            query=entity[cVar.CollectionProperty].AsBsonArray.AsQueryable().Where(cVar.Filter.ToString());
                        } else {
                            query=entity[cVar.CollectionProperty].AsBsonArray.AsQueryable();
                        }
                        if (query.Count() != 0) {
                            expression.Parameters[cVar.VariableName] = cVar.VariableType switch {
                                VariableType.NUMBER => query.Select($"e=>e.{cVar.Property}.AsDouble").FirstOrDefault() ,
                                VariableType.STRING => query.Select($"e=>e.{cVar.Property}.AsString").FirstOrDefault() ?? "",
                                VariableType.BOOLEAN => query.Select($"e=>e.{cVar.Property}.AsBoolean").FirstOrDefault(),
                                VariableType.DATE => query.Select(e => DateTime.Parse(e[cVar.Property].AsString)).FirstOrDefault(),
                                VariableType.LIST_NUMBER => query.Select(e => e[cVar.Property].AsDouble),
                                VariableType.LIST_STRING => query.Select(e => e[cVar.Property].AsString),
                                VariableType.LIST_BOOLEAN => query.Select(e => e[cVar.Property].AsBoolean),
                                VariableType.LIST_DATE => query.Select(e => DateTime.Parse(e[cVar.Property].AsString)),
                                _ => query.Select(e => e[cVar.Property].AsDouble)
                            };
                        } else {
                            expression.Parameters[cVar.VariableName] = cVar.VariableType switch {
                                VariableType.NUMBER => 0.00,
                                VariableType.STRING => "",
                                VariableType.BOOLEAN => false,
                                VariableType.DATE => DateTime.MinValue,
                                VariableType.LIST_NUMBER => new List<double>(){0,0,0},
                                VariableType.LIST_STRING => new List<string>(){"","",""},
                                VariableType.LIST_BOOLEAN => new List<bool>(){false,false,false},
                                VariableType.LIST_DATE => new List<DateTime>(){DateTime.MinValue,DateTime.MinValue,DateTime.MinValue},
                                _ => throw new ArgumentException("Empty Value type not supported")
                            };
                        }
                    }
                }else if (pVar is RefPropertyVariable rVar) { 
                    var collection = DB.Collection(rVar.DatabaseName,rVar.CollectionName).AsQueryable();
                    if (rVar.Filter != null) {
                        collection.Where(rVar.Filter.ToString());
                    }
                    if (collection.Any()) {
                        expression.Parameters[rVar.VariableName] = rVar.VariableType switch {
                            VariableType.NUMBER => collection.Select(e => e[rVar.Property].AsDouble).FirstOrDefault(),
                            VariableType.STRING => collection.Select(e => e[rVar.Property].AsString).FirstOrDefault(),
                            VariableType.BOOLEAN => collection.Select(e => e[rVar.Property].AsBoolean).FirstOrDefault(),
                            VariableType.DATE => collection.Select(e => DateTime.Parse(e[rVar.Property].AsString)).FirstOrDefault(),
                            VariableType.LIST_NUMBER => collection.Select(e => e[rVar.Property].AsDouble),
                            VariableType.LIST_STRING => collection.Select(e => e[rVar.Property].AsString),
                            VariableType.LIST_BOOLEAN => collection.Select(e => e[rVar.Property].AsBoolean),
                            VariableType.LIST_DATE => collection.Select(e => DateTime.Parse(e[rVar.Property].AsString)),
                            _ => throw new ArgumentException("Empty Value type not supported")
                        };
                    } else {
                        expression.Parameters[rVar.VariableName] = rVar.VariableType switch {
                            VariableType.NUMBER => 0.00,
                            VariableType.STRING => "",
                            VariableType.BOOLEAN => false,
                            VariableType.DATE => DateTime.MinValue,
                            VariableType.LIST_NUMBER => new List<double>(),
                            VariableType.LIST_STRING => new List<string>(),
                            VariableType.LIST_BOOLEAN => new List<bool>(),
                            VariableType.LIST_DATE => new List<DateTime>(),
                            _ => throw new ArgumentException("Empty Value type not supported")
                        };
                    }
                }else if (pVar is RefCollectionPropertyVariable rcVar) {
                    var collection = DB.Collection(rcVar.DatabaseName, rcVar.CollectionName);
                    List<BsonDocument> list = [];
                    if (rcVar.Filter != null) {
                        list=await collection.AsQueryable().Where(rcVar.Filter.ToString()).ToListAsync();
                    } else {
                        list=await collection.Find(_=>true).ToListAsync();
                    }
                    var query=list.SelectMany(e => e[rcVar.CollectionProperty].AsBsonArray).ToList();

                    if (rcVar.SubFilter != null) {
                        query.AsQueryable().Where(rcVar.SubFilter.ToString());
                    }
                    if (query.Count!=0) {
                        expression.Parameters[rcVar.VariableName] = rcVar.VariableType switch {
                            VariableType.NUMBER => query.Select(e => e[rcVar.Property].AsDouble).FirstOrDefault(),
                            VariableType.STRING => query.Select(e => e[rcVar.Property].AsString).FirstOrDefault(),
                            VariableType.BOOLEAN => query.Select(e => e[rcVar.Property].AsBoolean).FirstOrDefault(),
                            VariableType.DATE => query.Select(e => DateTime.Parse(e[rcVar.Property].AsString)).FirstOrDefault(),
                            VariableType.LIST_NUMBER => query.Select(e => e[rcVar.Property].AsDouble),
                            VariableType.LIST_STRING => query.Select(e => e[rcVar.Property].AsString),
                            VariableType.LIST_BOOLEAN => query.Select(e => e[rcVar.Property].AsBoolean),
                            VariableType.LIST_DATE => query.Select(e => DateTime.Parse(e[rcVar.Property].AsString)),
                            _ => throw new ArgumentException("Empty Value type not supported")
                        };
                    } else {
                        expression.Parameters[rcVar.VariableName] = rcVar.VariableType switch {
                            VariableType.NUMBER => 0.00,
                            VariableType.STRING => "",
                            VariableType.BOOLEAN => false,
                            VariableType.DATE => DateTime.MinValue,
                            VariableType.LIST_NUMBER => new List<double>(),
                            VariableType.LIST_STRING => new List<string>(),
                            VariableType.LIST_BOOLEAN => new List<bool>(),
                            VariableType.LIST_DATE => new List<DateTime>(),
                            _ => throw new ArgumentException("Empty Value type not supported")
                        };
                    }
                } else {
                    if(entity.Contains(pVar.Property)) {
                        expression.Parameters[pVar.VariableName] = entity[pVar.Property];
                    }
                }
            }
        }
        return expression;
    }
}

