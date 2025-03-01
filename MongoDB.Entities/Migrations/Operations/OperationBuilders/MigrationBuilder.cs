using System.Collections.Generic;
namespace MongoDB.Entities;

public class MigrationBuilder {
    public virtual List<IMigrationOperation> Operations { get; } = [];
    
    public virtual OperationBuilder<AddFieldOperation> AddField(
        string collectionName, string propertyName, Field field) {
        var operation = new AddFieldOperation {
            CollectionName = collectionName,
            PropertyName = propertyName,
            Field = field,
            IsDestructive = false
        };
        Operations.Add(operation);
        return new(operation);
    }

    public virtual OperationBuilder<DropFieldOperation> DropField(string collectionName, string propertyName, Field field) {
        var operation = new DropFieldOperation {
            CollectionName = collectionName,
            PropertyName = propertyName,
            Field = field,
            IsDestructive = true
        };
        Operations.Add(operation);
        return new OperationBuilder<DropFieldOperation>(operation);
    }
    
    public virtual OperationBuilder<AlterFieldOperation> AlterField(string collectionName, string propertyName, Field field, Field oldField) {
        var operation = new AlterFieldOperation {
            CollectionName = collectionName,
            PropertyName = propertyName,
            Field = field,
            OldField = new AddFieldOperation() {
                CollectionName = collectionName,
                PropertyName = propertyName,
                Field = oldField,
                IsDestructive = false
            },
            IsDestructive = true
        };
        Operations.Add(operation);
        return new OperationBuilder<AlterFieldOperation>(operation);
    }
}