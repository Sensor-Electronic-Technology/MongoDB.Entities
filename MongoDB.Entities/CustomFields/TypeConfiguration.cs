using System.Collections.Generic;

namespace MongoDB.Entities;
public class TypeConfiguration:Entity {
    public string CollectionName { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public List<Field> Fields { get; set; } = [];
    public Many<DocumentMigration,TypeConfiguration> Migrations { get; set; }
    public TypeConfiguration() {
        this.InitOneToMany(() => Migrations);
    }
}

public class ChildTypeConfiguration : TypeConfiguration {
    public string ChildProperty { get; set; } = null!;
}








