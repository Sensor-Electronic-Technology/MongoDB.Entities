namespace MongoDB.Entities;

public class OperationBuilder<TOperation> where TOperation:IMigrationOperation {
    protected virtual TOperation Operation { get; }
    public OperationBuilder(TOperation operation) {
        this.Operation = operation;
    }
}