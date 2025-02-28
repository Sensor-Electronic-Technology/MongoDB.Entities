using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Entities;

namespace MongoDB.Entities;

public class DocumentMigration : Entity,IDocumentMigration {
    public DateTime CreatedOn { get; set; }
    public DateTime MigratedOn { get; set; }
    public int MigrationNumber { get; set; }
    public bool IsMigrated { get; set; }
    //public Model ModelSnapshot { get; set; }
    public List<IMigrationOperation> UpOperations { get; set; } = [];
    public List<IMigrationOperation> DownOperations { get; set; } = [];
    public void Build(MigrationBuilder builder) {
        builder.Operations.ForEach(op => {
            if (op is AddFieldOperation addOp) {
                this.UpOperations.Add(addOp);
                this.DownOperations.Add(new DropFieldOperation() {
                    CollectionName = addOp.CollectionName,
                    Field = addOp.Field,
                    PropertyName = addOp.PropertyName,
                    IsDestructive = true
                });
            }else if (op is DropFieldOperation dropOp) {
                this.UpOperations.Add(dropOp);
                this.DownOperations.Add(new AddFieldOperation() {
                    CollectionName = dropOp.CollectionName,
                    Field = dropOp.Field,
                    PropertyName = dropOp.PropertyName,
                    IsDestructive = true
                });
            }else if (op is AlterFieldOperation alterOp) {
                this.UpOperations.Add(alterOp);
                this.DownOperations.Add(new AlterFieldOperation() {
                    CollectionName = alterOp.CollectionName,
                    Field = alterOp.Field,
                    PropertyName = alterOp.PropertyName,
                    IsDestructive = true,
                });
            }
        });
    }
}