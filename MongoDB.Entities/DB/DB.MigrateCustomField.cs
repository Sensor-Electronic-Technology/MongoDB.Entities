using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using NCalcExtensions;

namespace MongoDB.Entities;

public static partial class DB {
    
    public static async Task Migrate() {
        var migrateCollection = DB.Collection<DocumentMigration>();
        var migrations = await migrateCollection.Find(e => e.IsMigrated)
                                                .SortByDescending(e => e.MigrationNumber)
                                                .ToListAsync();

        foreach (var migration in migrations) {
            var typeConfig = await migration.TypeConfiguration.ToEntityAsync();
            var collection = DB.Collection(typeConfig.DatabaseName,typeConfig.CollectionName);
            var cursor=await collection.Find(new BsonDocument()).ToCursorAsync();
            List<UpdateManyModel<BsonDocument>> updates = [];
            List<DeleteManyModel<BsonDocument>> deletes = [];
            while (await cursor.MoveNextAsync()) {
                var batch = cursor.Current;
                foreach (var entity in batch) {
                    //Check if AdditionalData is null, create new if null
                    var doc=entity.GetElement("AdditionalData").Value.ToBsonDocument();
                    if (doc.Contains("_csharpnull")) {
                        doc = new BsonDocument();
                    }
                    foreach (var op in migration.UpOperations) {
                        if (op is AddFieldOperation addField) {
                            if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == addField.Field.FieldName) == null) {
                                AddField(typeConfig,addField.Field,ref doc,entity);
                            }
                        }else if (op is DropFieldOperation dropField) {
                            
                        }else if (op is AlterFieldOperation alterField) {
                
                        }
                    }
                    //Add update to queue
                    var filter=Builders<BsonDocument>.Filter.Eq("_id",entity["_id"]);
                    var update=Builders<BsonDocument>.Update.Set("AdditionalData",doc);
                    updates.Add(new(filter,update));
                }
            }
            await collection.BulkWriteAsync(updates);
        }
    }

    public static void AddField(TypeConfiguration typeConfig, Field field, ref BsonDocument doc,BsonDocument entity) {
        if (field is ObjectField oField) {
            var objDoc=new BsonDocument();
            foreach(var f in oField.Fields) { 
                AddField(typeConfig,f,ref objDoc,entity);
            }
            doc[oField.FieldName] = objDoc;
        }else if (field is ValueField vField) {
            doc.Add(vField.FieldName,BsonValue.Create(vField.DefaultValue));
        }else if (field is SelectionField sField) {
            doc.Add(sField.FieldName,BsonValue.Create(sField.DefaultValue));
        }else if (field is CalculatedField cField) {
            ProcessCalculationField(cField,ref doc,entity,out var expression);
        }
    }

    public static void ProcessCalculationField(CalculatedField cField, ref BsonDocument doc,BsonDocument entity,out ExtendedExpression expression) {
        expression = new ExtendedExpression(cField.Expression);
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
                    if (cVar.Filter != null) {
                        query=doc[cVar.CollectionProperty].AsBsonArray.AsQueryable().Where(cVar.Filter.ToString());
                    } else {
                        query = doc[cVar.CollectionProperty].AsBsonArray.AsQueryable();
                    }
                    
                    switch (cVar.ValueType) {
                        case ValueType.NUMBER:
                            expression.Parameters[cVar.Property]=query.Select(e => e[cVar.Property].AsDouble);
                            break;
                        case ValueType.STRING:
                            expression.Parameters[cVar.Property]=query.Select(e => e[cVar.Property].AsString);
                            break;
                        case ValueType.BOOLEAN:
                            expression.Parameters[cVar.Property]=query.Select(e => e[cVar.Property].AsBoolean);
                            break;
                        case ValueType.DATE:
                            expression.Parameters[cVar.Property]=query
                                .Select(e => DateTime.Parse(e[cVar.Property].AsString));
                            break;
                        default:
                            expression.Parameters[cVar.Property]=query.Select(e => e[cVar.Property].AsDouble);
                            break;
                    }
                }
            }else if (variable is ReferencePropertyVariable rVar) {
                var collection = DB.Collection(rVar.DatabaseName,rVar.CollectionName).AsQueryable();
                if (rVar.Filter != null) {
                    collection.Where(rVar.Filter.ToString());
                }
                if (collection.Any()) {
                    expression.Parameters[rVar.PropertyName] = rVar.ValueType switch {
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
                }
            }else if (variable is ReferenceCollectionVariable rcVar) {
                var collection = DB.Collection(rcVar.DatabaseName,rcVar.CollectionName).AsQueryable();
                if (rcVar.Filter != null) {
                    collection=collection.Where(rcVar.Filter.ToString());
                }
                var query=collection.Select(e => e[rcVar.CollectionProperty].AsBsonArray);
                if (collection.Any()) {
                    expression.Parameters[rcVar.Property] = rcVar.ValueType switch {
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
                }
            }
        }
    }
}

