namespace MongoDB.Entities;

public interface IMigrationOperation {
    public bool IsDestructive { get; set; }
}