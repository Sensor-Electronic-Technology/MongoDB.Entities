using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Entities;

namespace MongoDB.Entities;

[Collection("_document_migrations_")]
public class DocumentMigration : Entity,IDocumentMigration,ICreatedOn {
    public DateTime CreatedOn { get; set; }
    public DateTime MigratedOn { get; set; }
    public int MigrationNumber { get; set; }
    public bool IsMigrated { get; set; }
    public One<TypeConfiguration>? TypeConfiguration { get; set; }
    public DocumentVersion Version { get; set; }
    public List<FieldOperation> UpOperations { get; set; } = [];
    public List<FieldOperation> DownOperations { get; set; } = [];
    public void Build(MigrationBuilder builder) {
        builder.Operations.ForEach(op => {
            if (op is AddFieldOperation addOp) {
                this.UpOperations.Add(addOp);
                this.DownOperations.Add(new DropFieldOperation() {
                    Field = addOp.Field,
                    IsDestructive = true
                });
            }else if (op is DropFieldOperation dropOp) {
                this.UpOperations.Add(dropOp);
                this.DownOperations.Add(new AddFieldOperation() {
                    Field = dropOp.Field,
                    IsDestructive = true
                });
            }else if (op is AlterFieldOperation alterOp) {
                this.UpOperations.Add(alterOp);
                this.DownOperations.Add(new AlterFieldOperation() {
                    Field = alterOp.OldField,
                    OldField = alterOp.Field,
                });
            }
        });
    }
}