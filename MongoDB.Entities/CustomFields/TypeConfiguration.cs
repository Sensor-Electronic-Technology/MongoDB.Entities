using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

public class TypeConfigurationMap {
    public ObjectId _id { get; set; }
    public string? CollectionName { get; set; }
    public ICollection<TypeConfiguration> TypesConfigurations { get; set; } = [];
}

public class TypeConfiguration {
    /*public string? CollectionName { get; set; } //CollectionName( i.e. type)*/
    public string? PropertyName { get; set; } //BsonDocument PropertyName 
    public ICollection<Field>? Fields { get; set; }
}







