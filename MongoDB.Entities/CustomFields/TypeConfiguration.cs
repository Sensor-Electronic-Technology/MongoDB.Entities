using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;


[Collection("type_configurations"),
 BsonDiscriminator(RootClass = true), 
 BsonKnownTypes(typeof(EmbeddedTypeConfiguration))]
public class TypeConfiguration:Entity {
    public string CollectionName { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string TypeName { get; set; } = null!;
    public DocumentVersion DocumentVersion { get; set; } = new(0,0,0);
    public List<Field> Fields { get; set; } = [];
    public Many<DocumentMigration,TypeConfiguration> Migrations { get; set; }
    public TypeConfiguration() {
        this.InitOneToMany(() => Migrations);
    }
    static TypeConfiguration() {
        DB.Index<TypeConfiguration>()
          .Key(e => e.TypeName, KeyType.Text)
          .Option(o => o.Unique = true)
          .CreateAsync().Wait();
        
        DB.Index<TypeConfiguration>()
          .Key(e => e.CollectionName, KeyType.Text)
          .Option(o => o.Unique = true)
          .CreateAsync().Wait();
        
    }
}

public class EmbeddedTypeConfiguration : TypeConfiguration {
    public string ChildProperty { get; set; } = null!;
}








