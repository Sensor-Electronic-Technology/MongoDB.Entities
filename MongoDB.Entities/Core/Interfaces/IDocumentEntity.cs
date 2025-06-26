using MongoDB.Bson;

namespace MongoDB.Entities;

/// <summary>
/// Inherit this interface to use a custom ID with AdditionalData
/// </summary>
public interface IDocumentEntity:IEntity {
    public BsonDocument? AdditionalData { get; set; }
    public DocumentVersion Version { get; set; }
}