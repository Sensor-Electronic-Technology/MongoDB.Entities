using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Entities;

/// <summary>
/// Inherit this interface to use a custom ID with AdditionalData
/// </summary>
public interface IDocumentEntity:IEntity {
    public BsonDocument? AdditionalData { get; set; }
    public DocumentVersion Version { get; set; }
}

public interface IHasEmbedded {
    public void UpdateEmbedded(IDocumentEntity entity);
    public Task ApplyEmbeddedMigrations();
}