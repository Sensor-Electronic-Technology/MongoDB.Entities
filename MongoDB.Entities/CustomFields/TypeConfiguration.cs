using System.Collections.Generic;
using System.Collections.ObjectModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

/*public class TypeConfigurationMap:Entity {
    public List<Field>? Fields { get; set; }
}*/

/*public class TypeConfigurationMap:Entity {
    public string CollectionName { get; set; } = null!;
    public Many<TypeConfiguration,TypeConfigurationMap> TypeConfigurations { get; set; }

    public TypeConfigurationMap() {
        this.InitOneToMany(() => this.TypeConfigurations);
    }
}*/

public class TypeConfiguration:Entity {
    public string CollectionName { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public IList<Field> Fields { get; set; } = [];
    public Many<DocumentMigration,TypeConfiguration> Migrations { get; set; }
    public TypeConfiguration() {
        this.InitOneToMany(() => Migrations);
    }
}

public class ChildTypeConfiguration : TypeConfiguration {
    public string ChildProperty { get; set; } = null!;
}








