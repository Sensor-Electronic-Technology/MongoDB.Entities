namespace MongoDB.Entities;

public interface IFieldOperation : IMigrationOperation {
    public Field Field { get; set; }
}