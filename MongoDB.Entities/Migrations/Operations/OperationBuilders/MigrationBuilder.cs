using System;
using System.Collections.Generic;
namespace MongoDB.Entities;

public class MigrationBuilder {
    public virtual List<FieldOperation> Operations { get; } = [];
    
    public virtual OperationBuilder<AddFieldOperation> AddField(Field field) {
        var operation = new AddFieldOperation {
            Field = field,
            IsDestructive = false
        };
        Operations.Add(operation);
        return new(operation);
    }

    public virtual OperationBuilder<DropFieldOperation> DropField(Field field) {
        var operation = new DropFieldOperation {
            Field = field,
            IsDestructive = true
        };
        Operations.Add(operation);
        return new(operation);
    }
    
    public virtual OperationBuilder<AlterFieldOperation> AlterField(Field field, Field oldField) {
        var operation = new AlterFieldOperation {
            Field = field,
            OldField = oldField,
            IsDestructive = true
        };
        Operations.Add(operation);
        return new(operation);
    }

    public virtual DocumentMigration Build() {
        DocumentMigration migration = new DocumentMigration() {
            MigratedOn = DateTime.MinValue.ToUniversalTime(),
            IsMigrated = false,
            MigrationNumber = 0,
        };
        migration.Build(this);
        return migration;
    }
}