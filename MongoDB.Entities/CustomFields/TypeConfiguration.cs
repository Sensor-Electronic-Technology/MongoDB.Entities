using System.Collections.Generic;
using System.Collections.ObjectModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

/*public class TypeConfigurationMap:Entity {
    public List<Field>? Fields { get; set; }
}*/

public class TypeConfigurationMap:Entity {
    public string Type { get; set; } = null!;
    public Many<TypeConfiguration,TypeConfigurationMap> TypeConfigurations { get; set; }

    public TypeConfigurationMap() {
        this.InitOneToMany(() => this.TypeConfigurations);
    }
}

public class TypeConfiguration:Entity {
    public string Type { get; set; } = null!;
    public One<TypeConfigurationMap> TypeConfigMap { get; set; }
    public Many<Field,TypeConfiguration> Fields { get; set; }
    public TypeConfiguration() {
        this.InitOneToMany(() => Fields);
    }
}

public class ChildTypeConfiguration : TypeConfiguration {
    public string ChildCollectionProperty { get; set; } = null!;
}








