using MongoDB.Bson;

namespace MongoDB.Entities;

public interface ICustomData {
    public BsonDocument AdditionalData { get; set; }
}