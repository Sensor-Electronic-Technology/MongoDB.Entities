using System;
using System.Collections.Generic;
using System.Linq;

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

    public virtual DocumentMigration Build(TypeConfiguration typeConfig,int migrationNumber) {
        DocumentMigration migration = new DocumentMigration {
            MigratedOn = DateTime.MinValue.ToUniversalTime(),
            IsMigrated = false,
            MigrationNumber = 0,
        };
        migration.Build(this);
        bool major=migration.UpOperations.OfType<AddFieldOperation>().Any();
        major= major || migration.UpOperations.OfType<DropFieldOperation>().Any();
        if (major) {
            migration.Version=typeConfig.DocumentVersion.IncrementMajor();
            migration.IsMajorVersion = true;
        } else {
            migration.Version=typeConfig.DocumentVersion.Increment();
            migration.IsMajorVersion=migration.Version.Major>typeConfig.DocumentVersion.Major;
        }
        migration.TypeConfiguration = typeConfig.ToReference();
        migration.MigrationNumber = ++migrationNumber;
        return migration;
    }
    
    public virtual EmbeddedMigration Build(EmbeddedTypeConfiguration typeConfig,int migrationNumber) {
        EmbeddedMigration migration = new EmbeddedMigration() {
            MigratedOn = DateTime.MinValue.ToUniversalTime(),
            IsMigrated = false,
            MigrationNumber = 0,
        };
        migration.Build(this);
        bool major=migration.UpOperations.OfType<AddFieldOperation>().Any();
        major= major || migration.UpOperations.OfType<DropFieldOperation>().Any();
        if (major) {
            migration.Version=typeConfig.DocumentVersion.IncrementMajor();
            migration.IsMajorVersion = true;
        } else {
            migration.Version=typeConfig.DocumentVersion.Increment();
            migration.IsMajorVersion=migration.Version.Major>typeConfig.DocumentVersion.Major;
        }
        migration.EmbeddedTypeConfiguration = typeConfig.ToReference();
        migration.MigrationNumber = ++migrationNumber;
        return migration;
    }
}