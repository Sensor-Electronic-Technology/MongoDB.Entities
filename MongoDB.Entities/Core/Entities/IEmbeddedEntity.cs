using MongoDB.Bson;

namespace MongoDB.Entities;

public interface IEmbeddedEntity {
    public BsonDocument AdditionalData { get; set; }
}