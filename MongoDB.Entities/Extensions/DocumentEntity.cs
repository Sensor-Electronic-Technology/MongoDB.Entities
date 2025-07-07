using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;

namespace MongoDB.Entities;

public static partial class Extensions {
    public static async Task ApplyMigrations<TEntity>(this TEntity entity,
                                                      Dictionary<string, object>? additionalData = null,
                                                      CancellationToken cancellation = default)
        where TEntity : IDocumentEntity {
        var typeConfig = DB.TypeConfiguration<TEntity>() ??
                         await DB.Find<DocumentTypeConfiguration>()
                                 .Match(e => e.CollectionName==Cache<TEntity>.CollectionName)
                                 .ExecuteFirstAsync(cancellation);

        if (typeConfig != null && typeConfig.Migrations.Any()) {
            await EntityApplyDocMigration(entity, typeConfig, additionalData, cancellation);
        }

        if (typeof(IHasEmbedded).IsAssignableFrom(typeof(TEntity))) {
            await ((IHasEmbedded)entity).ApplyEmbeddedMigrations();
        }
    }

    public static async Task ApplyMigrations<TEntity>(this IEnumerable<TEntity> entities,
                                                      List<Dictionary<string, object>>? additionalData = null,
                                                      CancellationToken cancellation = default)
        where TEntity : IDocumentEntity {
        var typeConfig = DB.TypeConfiguration<TEntity>() ??
                         await DB.Find<DocumentTypeConfiguration>()
                                 .Match(e => e.CollectionName == DB.CollectionName<TEntity>())
                                 .ExecuteFirstAsync(cancellation);

        if (typeConfig != null && typeConfig.Migrations.Any()) {
            var documentEntities = entities as TEntity[] ?? entities.ToArray();
            if (additionalData != null) {
                if (documentEntities.Count() == additionalData.Count) {
                    for (var i = 0; i < documentEntities.Count(); i++) {
                        await EntityApplyDocMigration(documentEntities[i], typeConfig, additionalData[i], cancellation);
                        if (typeof(IHasEmbedded).IsAssignableFrom(typeof(TEntity))) {
                            await ((IHasEmbedded)documentEntities[i]).ApplyEmbeddedMigrations();
                        }
                    }
                }
            } else {
                foreach (var entity in documentEntities) {
                    await EntityApplyDocMigration(entity, typeConfig, cancellation: cancellation);
                    if (typeof(IHasEmbedded).IsAssignableFrom(typeof(TEntity))) {
                        await ((IHasEmbedded)entity).ApplyEmbeddedMigrations();
                    }
                }
            }
        }
    }
    
    internal static async Task EntityApplyDocMigration<TEntity>(TEntity entity,
                                                                DocumentTypeConfiguration typeConfig,
                                                                Dictionary<string, object>? additionalData = null,
                                                                CancellationToken cancellation = default)
        where TEntity : IDocumentEntity {
        var migrations = await typeConfig.Migrations
                                         .ChildrenQueryable()
                                         .ToListAsync(cancellationToken: cancellation);

        if (migrations.Count <= 0) {
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