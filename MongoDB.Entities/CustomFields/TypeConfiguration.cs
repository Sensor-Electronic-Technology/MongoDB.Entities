using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

[BsonDiscriminator(RootClass = true), BsonKnownTypes(typeof(EmbeddedTypeConfiguration))]
public class TypeConfiguration:Entity {
    public string CollectionName { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public List<Field> Fields { get; set; } = [];
    public Many<DocumentMigration,TypeConfiguration> Migrations { get; set; }
    public TypeConfiguration() {
        this.InitOneToMany(() => Migrations);
        DB.Index<TypeConfiguration>()
          .Key(e => e.CollectionName, KeyType.Text)
          .Option(o => o.Unique = true);
    }
}

public class EmbeddedTypeConfiguration : TypeConfiguration {
    public string ChildProperty { get; set; } = null!;
}








