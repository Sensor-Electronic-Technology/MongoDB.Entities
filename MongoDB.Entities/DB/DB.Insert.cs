using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public static partial class DB {
    /// <summary>
    /// Inserts a new entity into the collection.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The instance to persist</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">And optional cancellation token</param>
    public static Task InsertAsync<T>(T entity, IClientSessionHandle? session = null,
                                      CancellationToken cancellation = default) where T : IEntity {
        PrepAndCheckIfInsert(entity);

        return session == null
                   ? Collection<T>().InsertOneAsync(entity, null, cancellation)
                   : Collection<T>().InsertOneAsync(session, entity, null, cancellation);
    }

    /// <summary>
    /// Inserts a new entity into the collection.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The instance to persist</param>
    /// <param name="additionalData">migration data</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">And optional cancellation token</param>
    public static async Task InsertMigrateAsync<T>(T entity,Dictionary<string,object>? additionalData=null,
                                                   IClientSessionHandle? session = null, 
                                                   CancellationToken cancellation = default) where T : IDocumentEntity {
        PrepAndCheckIfInsert(entity);
        await entity.ApplyMigrations(additionalData, cancellation);
        if (session == null) {
            await Collection<T>().InsertOneAsync(entity, null, cancellation);
        } else {
            await Collection<T>().InsertOneAsync(session, entity, null, cancellation);
        }
    }

    /// <summary>
    /// Inserts a batch of new entities into the collection.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entities">The entities to persist</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">And optional cancellation token</param>
    public static Task<BulkWriteResult<T>> InsertAsync<T>(IEnumerable<T> entities,
                                                          IClientSessionHandle? session = null,
                                                          CancellationToken cancellation = default) where T : IEntity {
        var models = new List<WriteModel<T>>(entities.Count());

        foreach (var ent in entities) {
            PrepAndCheckIfInsert(ent);
            models.Add(new InsertOneModel<T>(ent));
        }

        return session == null
                   ? Collection<T>().BulkWriteAsync(models, _unOrdBlkOpts, cancellation)
                   : Collection<T>().BulkWriteAsync(session, models, _unOrdBlkOpts, cancellation);
    }

    /// <summary>
    /// Inserts a batch of new entities into the collection.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entities">The entities to persist</param>
    /// <param name="additionalData">Migration data</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">And optional cancellation token</param>
    public static async Task<BulkWriteResult<T>> InsertMigrateAsync<T>(IEnumerable<T> entities,
                                                                       List<Dictionary<string, object>>? additionalData = null,
                                                                       IClientSessionHandle? session = null,
                                                                       CancellationToken cancellation = default) where T : IDocumentEntity {
        var models = new List<WriteModel<T>>(entities.Count());
        await entities.SaveMigrateAsync(additionalData, session, cancellation);
        foreach (var ent in entities) {
            PrepAndCheckIfInsert(ent);
            models.Add(new InsertOneModel<T>(ent));
        }
        return session == null
                   ? await Collection<T>().BulkWriteAsync(models, _unOrdBlkOpts, cancellation)
                   : await Collection<T>().BulkWriteAsync(session, models, _unOrdBlkOpts, cancellation);
    }
    
}