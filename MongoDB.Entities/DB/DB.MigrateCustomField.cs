using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

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
                foreach (var obj in batch) {
                    foreach (var op in migration.UpOperations) {
                        if (op is AddFieldOperation addField) {
                            if (typeConfig.Fields.FirstOrDefault(e => e.FieldName == addField.Field.FieldName) == null) {
                                await AddField(typeConfig,addField.Field,migration);
                            }
                                
                            
                        }else if (op is DropFieldOperation dropField) {
                
                        }else if (op is AlterFieldOperation alterField) {
                
                        }
                    }
                }
            }
            await cursor.ForEachAsync(
                obj => {
                    

                });

        }
    }

    public static async Task AddField(TypeConfiguration typeConfig,
                                      Field field,
                                      DocumentMigration migration) {
        var logger = CreateLogger;
        if (field is ObjectField oField) {
            var exist=typeConfig.Fields.OfType<ObjectField>().FirstOrDefault(e => e.FieldName == oField.FieldName)!=null;
            if (!exist) {
                
            } else {
                //error
                logger.LogError("In TypeConfiguration for {CollectionName} , Field {FieldName} already exists",
                    typeConfig.CollectionName,field.FieldName);
            }
        }else if (field is ValueField vField) {
            
        }else if (field is SelectionField sField) {
            
        }else if (field is CalculatedField cField) {
            
        }
        
    }
}