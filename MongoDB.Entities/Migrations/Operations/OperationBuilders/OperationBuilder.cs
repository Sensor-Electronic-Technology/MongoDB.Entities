namespace MongoDB.Entities;

public class OperationBuilder<TOperation> where TOperation:FieldOperation {
    protected virtual TOperation Operation { get; }
    public OperationBuilder(TOperation operation) {
        Operation = operation;
    }
}