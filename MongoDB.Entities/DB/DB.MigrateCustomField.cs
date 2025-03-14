using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NCalcExtensions;

namespace MongoDB.Entities;

public static partial class DB {
    public static async Task MigrateFields() {
        var migrateCollection = Collection<DocumentMigration>();
        var migrations = await migrateCollection.Find(e => !e.IsMigrated).SortByDescending(e => e.MigrationNumber).ToListAsync();
        foreach (var migration in migrations) {
            if (migration.TypeConfiguration == null) {
                //CreateLogger.LogError("TypeConfiguration is null");
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
                    var update=Builders<BsonDocument>.Update.Set("AdditionalData",doc);
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
                doc.Add(calcField.FieldName,BsonValue.Create(expression.Evaluate()));
            } else {
                doc.Add(vField.FieldName,BsonValue.Create(vField.DefaultValue));
            }
        }else if (field is SelectionField sField) {
            doc.Add(sField.FieldName,BsonValue.Create(sField.DefaultValue));
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
    
    public static async Task<ExtendedExpression> ProcessCalculationField(CalculatedField cField, BsonDocument doc,BsonDocument entity) {
        var expression = new ExtendedExpression(cField.Expression);
        foreach (var variable in cField.Variables) {
            if (variable is ValueVariable vVar) {
                expression.Parameters[vVar.VariableName] = vVar.Value;
            }else if (variable is PropertyVariable pVar) {
                if(entity.Contains(pVar.PropertyName)) {
                    expression.Parameters[pVar.VariableName] = entity[pVar.PropertyName];
                }
            }else if (variable is CollectionVariable cVar) {
                if (entity.Contains(cVar.CollectionProperty)) {
                    IQueryable<BsonValue>? query;
                    Console.WriteLine(cVar.Filter?.ToString() ?? "Empty Filter");
                    if (cVar.Filter != null) {
                        query=entity[cVar.CollectionProperty].AsBsonArray.AsQueryable().Where(cVar.Filter.ToString());
                    } else {
                        query=entity[cVar.CollectionProperty].AsBsonArray.AsQueryable();
                    }
                    if (query.Count() != 0) {
                        expression.Parameters[cVar.VariableName] = cVar.ValueType switch {
                            ValueType.NUMBER => query.Select($"e=>e.{cVar.Property}.AsDouble").FirstOrDefault() ,
                            ValueType.STRING => query.Select($"e=>e.{cVar.Property}.AsString").FirstOrDefault() ?? "",
                            ValueType.BOOLEAN => query.Select($"e=>e.{cVar.Property}.AsBoolean").FirstOrDefault(),
                            ValueType.DATE => query.Select(e => DateTime.Parse(e[cVar.Property].AsString)).FirstOrDefault(),
                            ValueType.LIST_NUMBER => query.Select(e => e[cVar.Property].AsDouble),
                            ValueType.LIST_STRING => query.Select(e => e[cVar.Property].AsString),
                            ValueType.LIST_BOOLEAN => query.Select(e => e[cVar.Property].AsBoolean),
                            ValueType.LIST_DATE => query.Select(e => DateTime.Parse(e[cVar.Property].AsString)),
                            _ => query.Select(e => e[cVar.Property].AsDouble)
                        };
                    } else {
                        expression.Parameters[cVar.VariableName] = cVar.ValueType switch {
                            ValueType.NUMBER => 0.00,
                            ValueType.STRING => "",
                            ValueType.BOOLEAN => false,
                            ValueType.DATE => DateTime.MinValue,
                            ValueType.LIST_NUMBER => new List<double>(){0,0,0},
                            ValueType.LIST_STRING => new List<string>(){"","",""},
                            ValueType.LIST_BOOLEAN => new List<bool>(){false,false,false},
                            ValueType.LIST_DATE => new List<DateTime>(){DateTime.MinValue,DateTime.MinValue,DateTime.MinValue},
                            _ => throw new ArgumentException("Empty Value type not supported")
                        };
                    }
                }
            }else if (variable is ReferencePropertyVariable rVar) {
                var collection = DB.Collection(rVar.DatabaseName,rVar.CollectionName).AsQueryable();
                if (rVar.Filter != null) {
                    collection.Where(rVar.Filter.ToString());
                }
                if (collection.Any()) {
                    expression.Parameters[rVar.VariableName] = rVar.ValueType switch {
                        ValueType.NUMBER => collection.Select(e => e[rVar.PropertyName].AsDouble).FirstOrDefault(),
                        ValueType.STRING => collection.Select(e => e[rVar.PropertyName].AsString).FirstOrDefault(),
                        ValueType.BOOLEAN => collection.Select(e => e[rVar.PropertyName].AsBoolean).FirstOrDefault(),
                        ValueType.DATE => collection.Select(e => DateTime.Parse(e[rVar.PropertyName].AsString)).FirstOrDefault(),
                        ValueType.LIST_NUMBER => collection.Select(e => e[rVar.PropertyName].AsDouble),
                        ValueType.LIST_STRING => collection.Select(e => e[rVar.PropertyName].AsString),
                        ValueType.LIST_BOOLEAN => collection.Select(e => e[rVar.PropertyName].AsBoolean),
                        ValueType.LIST_DATE => collection.Select(e => DateTime.Parse(e[rVar.PropertyName].AsString)),
                        _ => throw new ArgumentException("Empty Value type not supported")
                    };
                } else {
                    expression.Parameters[rVar.VariableName] = rVar.ValueType switch {
                        ValueType.NUMBER => 0.00,
                        ValueType.STRING => "",
                        ValueType.BOOLEAN => false,
                        ValueType.DATE => DateTime.MinValue,
                        ValueType.LIST_NUMBER => new List<double>(),
                        ValueType.LIST_STRING => new List<string>(),
                        ValueType.LIST_BOOLEAN => new List<bool>(),
                        ValueType.LIST_DATE => new List<DateTime>(),
                        _ => throw new ArgumentException("Empty Value type not supported")
                    };
                }
            }else if (variable is ReferenceCollectionVariable rcVar) {
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
                    expression.Parameters[rcVar.VariableName] = rcVar.ValueType switch {
                        ValueType.NUMBER => query.Select(e => e[rcVar.Property].AsDouble).FirstOrDefault(),
                        ValueType.STRING => query.Select(e => e[rcVar.Property].AsString).FirstOrDefault(),
                        ValueType.BOOLEAN => query.Select(e => e[rcVar.Property].AsBoolean).FirstOrDefault(),
                        ValueType.DATE => query.Select(e => DateTime.Parse(e[rcVar.Property].AsString)).FirstOrDefault(),
                        ValueType.LIST_NUMBER => query.Select(e => e[rcVar.Property].AsDouble),
                        ValueType.LIST_STRING => query.Select(e => e[rcVar.Property].AsString),
                        ValueType.LIST_BOOLEAN => query.Select(e => e[rcVar.Property].AsBoolean),
                        ValueType.LIST_DATE => query.Select(e => DateTime.Parse(e[rcVar.Property].AsString)),
                        _ => throw new ArgumentException("Empty Value type not supported")
                    };
                } else {
                    expression.Parameters[rcVar.VariableName] = rcVar.ValueType switch {
                        ValueType.NUMBER => 0.00,
                        ValueType.STRING => "",
                        ValueType.BOOLEAN => false,
                        ValueType.DATE => DateTime.MinValue,
                        ValueType.LIST_NUMBER => new List<double>(),
                        ValueType.LIST_STRING => new List<string>(),
                        ValueType.LIST_BOOLEAN => new List<bool>(),
                        ValueType.LIST_DATE => new List<DateTime>(),
                        _ => throw new ArgumentException("Empty Value type not supported")
                    };
                }
            }
        }
        return expression;
    }
}

