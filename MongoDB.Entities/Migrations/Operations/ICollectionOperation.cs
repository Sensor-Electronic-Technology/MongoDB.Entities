namespace MongoDB.Entities;

public interface ICollectionOperation {
    public string CollectionName { get; set; }
    public string PropertyName { get; set; }
}