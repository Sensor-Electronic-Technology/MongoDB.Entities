using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Entities;

namespace MongoDB.Entities;

[Collection("_document_migrations_")]
public class DocumentMigration : Entity,IDocumentMigration {
    public DateTime CreatedOn { get; set; }
    public DateTime MigratedOn { get; set; }
    public int MigrationNumber { get; set; }
    public bool IsMigrated { get; set; }
    public One<TypeConfiguration> TypeConfiguration { get; set; } = null!;
    public List<IMigrationOperation> UpOperations { get; set; } = [];
    public List<IMigrationOperation> DownOperations { get; set; } = [];
    public void Build(MigrationBuilder builder) {
        builder.Operations.ForEach(op => {
            if (op is AddFieldOperation addOp) {
                this.UpOperations.Add(addOp);
                this.DownOperations.Add(new DropFieldOperation() {
                    /*CollectionName = addOp.CollectionName,
                    PropertyName = addOp.PropertyName,*/
                    Field = addOp.Field,
                    
                    IsDestructive = true
                });
            }else if (op is DropFieldOperation dropOp) {
                this.UpOperations.Add(dropOp);
                this.DownOperations.Add(new AddFieldOperation() {
                    /*CollectionName = dropOp.CollectionName,
                    PropertyName = dropOp.PropertyName,*/
                    Field = dropOp.Field,
                    IsDestructive = true
                });
            }else if (op is AlterFieldOperation alterOp) {
                this.UpOperations.Add(alterOp);
                this.DownOperations.Add(new AlterFieldOperation() {
                    /*CollectionName = alterOp.CollectionName,
                    PropertyName = alterOp.PropertyName,*/
                    Field = alterOp.Field,
                    IsDestructive = true,
                    OldField = new AddFieldOperation() {
                        /*CollectionName = alterOp.CollectionName,
                        PropertyName = alterOp.PropertyName,*/
                        Field = alterOp.OldField.Field,
                        IsDestructive = true
                    }
                });
            }
        });
    }
}