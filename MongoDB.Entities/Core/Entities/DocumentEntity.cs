using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

/// <summary>
/// Inherit this class for managed ID and Additional data
/// </summary>
public abstract class DocumentEntity : IDocumentEntity {
    /// <summary>
    /// This property is auto managed. A new ID will be assigned for new entities upon saving.
    /// </summary>
    [BsonId, AsObjectId]
    public string ID { get; set; } = null!;

    /// <summary>
    /// Override this method in order to control the generation of IDs for new entities.
    /// </summary>
    public virtual object GenerateNewID()
        => ObjectId.GenerateNewId().ToString()!;

    /// <inheritdoc />
    public virtual bool HasDefaultID()
        => string.IsNullOrEmpty(ID);
    public BsonDocument? AdditionalData { get; set; }
    public DocumentVersion Version { get; set; }
}
