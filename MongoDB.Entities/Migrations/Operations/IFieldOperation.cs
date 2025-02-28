namespace MongoDB.Entities;

public interface IFieldOperation : IMigrationOperation, ICollectionOperation {
    public Field Field { get; set; }
}