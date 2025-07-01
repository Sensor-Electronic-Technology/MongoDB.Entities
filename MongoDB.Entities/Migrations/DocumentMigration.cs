using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

[Collection("_entity_migrations_"),
 BsonDiscriminator(RootClass = true), 
 BsonKnownTypes(typeof(DocumentMigration),typeof(EmbeddedMigration))]
public class EntityMigration : Entity, IDocumentMigration, ICreatedOn {
    public DateTime CreatedOn { get; set; }
    public DateTime MigratedOn { get; set; }
    public int MigrationNumber { get; set; }
    public bool IsMajorVersion { get; set; }
    public bool IsMigrated { get; set; }
    public DocumentVersion Version { get; set; }
    public List<FieldOperation> UpOperations { get; set; } = [];
    public List<FieldOperation> DownOperations { get; set; } = [];
    public void Build(MigrationBuilder builder) {
        builder.Operations.ForEach(op => {
            switch (op) {
                case AddFieldOperation addOp:
                    UpOperations.Add(addOp);
                    DownOperations.Add(new DropFieldOperation {
                        Field = addOp.Field,
                        IsDestructive = true
                    });
                    break;
                case DropFieldOperation dropOp:
                    UpOperations.Add(dropOp);
                    DownOperations.Add(new AddFieldOperation {
                        Field = dropOp.Field,
                        IsDestructive = true
                    });
                    break;
                case AlterFieldOperation alterOp:
                    UpOperations.Add(alterOp);
                    DownOperations.Add(new AlterFieldOperation {
                        Field = alterOp.OldField,
                        OldField = alterOp.Field,
                    });
                    break;
            }
        });
    }
}

[Collection("_entity_migrations_")]
public class DocumentMigration : EntityMigration {
    public One<DocumentTypeConfiguration>? TypeConfiguration { get; set; }
}

[Collection("_entity_migrations_")]
public class EmbeddedMigration : EntityMigration {
    public One<EmbeddedTypeConfiguration>? EmbeddedTypeConfiguration { get; set; }
    public string ParentTypeName { get; set; } = null!;
}   