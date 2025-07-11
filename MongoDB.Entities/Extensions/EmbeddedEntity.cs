﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Linq;

namespace MongoDB.Entities;

public static partial class Extensions {
    public static async Task ApplyEmbedded<TEmbedded>(this TEmbedded embedded,
                                                      Type parent,
                                                      Dictionary<string, object>? additionalData = null,
                                                      CancellationToken cancellation = default)
        where TEmbedded : IEmbeddedEntity {
        var embeddedConfig = DB.EmbeddedTypeConfiguration<TEmbedded>() ??
                             await DB.Find<EmbeddedTypeConfiguration>()
                                     .Match(e => e.FieldDefinitions.ContainsKey(parent.Name))
                                     .ExecuteFirstAsync(cancellation);

        if (embeddedConfig != null && embeddedConfig.EmbeddedMigrations.Any()) {
            await EmbeddedMigration(embedded, embeddedConfig, additionalData, cancellation);
        }
    }
    
    public static async Task ApplyEmbedded<TEmbedded>(this IEnumerable<TEmbedded> embedded,
                                                      Type parent,
                                                      List<Dictionary<string, object>>? additionalData = null,
                                                      CancellationToken cancellation = default)
        where TEmbedded : IEmbeddedEntity {
        var embeddedConfig = DB.EmbeddedTypeConfiguration<TEmbedded>() ??
                             await DB.Find<EmbeddedTypeConfiguration>()
                                     .Match(e => e.FieldDefinitions.ContainsKey(parent.Name))
                                     .ExecuteFirstAsync(cancellation);
        if (embeddedConfig != null && embeddedConfig.EmbeddedMigrations.Any()) {
            var entities = embedded as TEmbedded[] ?? embedded.ToArray();

            if (entities.Length == additionalData?.Count) {
                for (var i = 0; i < entities.Length; i++) {
                    await EmbeddedMigration(entities[i], embeddedConfig, additionalData[i], cancellation: cancellation);
                }
            } else {
                foreach (var embed in entities) {
                    await EmbeddedMigration(embed, embeddedConfig, cancellation: cancellation);
                }
            }
        }
    }

    internal static async Task EmbeddedMigration<TEmbedded>(TEmbedded entity,
                                                            EmbeddedTypeConfiguration typeConfig,
                                                            Dictionary<string, object>? additionalData = null,
                                                            CancellationToken cancellation = default)
        where TEmbedded : IEmbeddedEntity {
        var migrations = await typeConfig.EmbeddedMigrations
                                         .ChildrenQueryable()
                                         .ToListAsync(cancellationToken: cancellation);

        if (migrations.Count == 0) {
            return;
        }

        if (entity.AdditionalData == null) {
            entity.AdditionalData = [];
        }

        var doc = entity.AdditionalData;
        var entityDoc = entity.ToBsonDocument();

        foreach (var migration in migrations) {
            foreach (var op in migration.UpOperations) {
                if (op is AddFieldOperation addField) {
                    await DB.AddField(addField.Field, doc, entityDoc);
                } else if (op is DropFieldOperation dropField) {
                    doc.Remove(dropField.Field.FieldName);
                } else if (op is AlterFieldOperation alterField) {
                    doc.Remove(alterField.OldField.FieldName);
                    await DB.AddField(alterField.Field, doc, entityDoc);
                }
            }
        }

        if (additionalData != null) {
            foreach (var fieldItem in additionalData) {
                if (doc.Contains(fieldItem.Key)) {
                    doc[fieldItem.Key] = BsonValue.Create(fieldItem.Value);
                }
            }
        }
        entity.AdditionalData = doc;
    }
}






    /*internal static async Task EntityApplyEmbeddedMigration<TEntity>(TEntity entity,
                                                                     EmbeddedTypeConfiguration typeConfig,
                                                                     CancellationToken cancellation = default)
        where TEntity : IDocumentEntity {
        var migrations = await typeConfig.EmbeddedMigrations.ChildrenQueryable()
                                         .ToListAsync(cancellationToken: cancellation);

        if (migrations.Count == 0) {
            return;
        }

        var docEntity = entity.ToBsonDocument();

        foreach (var migration in migrations) {
            if (!typeConfig.FieldDefinitions.TryGetValue(migration.ParentTypeName, out var fieldDef)) {
                return;
            }

            foreach (var propertyName in fieldDef.PropertyNames) {
                if (!docEntity.Contains(propertyName)) {
                    continue;
                }

                if (fieldDef.IsArray) {
                    var arr = docEntity.GetElement(propertyName).Value.AsBsonArray;
                    if (arr == null || arr.Count == 0 || arr.Contains("_csharpnull")) {
                        continue;
                    }

                    foreach (var bVal in arr) {
                        await ApplyEmbeddedMigrationOperationsInline(bVal.AsBsonDocument, migration);
                    }

                    docEntity[propertyName] = arr;
                } else {
                    var doc = docEntity.GetElement(propertyName).Value.AsBsonDocument;
                    if (doc == null || doc.Contains("_csharpnull")) {
                        continue;
                    }

                    await ApplyEmbeddedMigrationOperationsInline(doc, migration);
                    docEntity[propertyName] = doc;
                }
            }
        }

        var modified = BsonSerializer.Deserialize<TEntity>(docEntity.ToBson());
        ((IHasEmbedded)entity).UpdateEmbedded(modified);
    }*/

    /*internal static async Task ApplyEmbeddedMigrationOperationsInline(BsonDocument embeddedDoc,
                                                                      EmbeddedMigration migration) {
        var addDataDoc = embeddedDoc.GetElement("AdditionalData").Value.ToBsonDocument();

        if (addDataDoc.Contains("_csharpnull")) {
            addDataDoc = [];
        }

        foreach (var op in migration.UpOperations) {
            if (op is AddFieldOperation addField) {
                await DB.AddField(addField.Field, addDataDoc, embeddedDoc);
            } else if (op is DropFieldOperation dropField) {
                if (addDataDoc.Contains(dropField.Field.FieldName)) {
                    addDataDoc.Remove(dropField.Field.FieldName);
                }
            } else if (op is AlterFieldOperation alterField) {
                if (addDataDoc.Contains(alterField.OldField.FieldName)) {
                    addDataDoc.Remove(alterField.OldField.FieldName);
                }
                await DB.AddField(alterField.Field, addDataDoc, embeddedDoc);
            }
        }

        embeddedDoc["AdditionalData"] = addDataDoc;
    }*/