namespace MongoDB.Entities;

public interface IEntityBuilder {
    public IValueFieldBuilder HasValueField(string name);
    public IObjectFieldBuilder HasObjectField(string name);
    public ISelectionFieldBuilder HasSelectionField(string name);
    public ICalculatedFieldBuilder HasCalculatedField(string name);
}

public class EntityBuilder:IEntityBuilder {
    public IFieldBuilder _fieldBuilder;
    
    public IValueFieldBuilder HasValueField(string name)
        => throw new System.NotImplementedException();

    public IObjectFieldBuilder HasObjectField(string name)
        => throw new System.NotImplementedException();

    public ISelectionFieldBuilder HasSelectionField(string name)
        => throw new System.NotImplementedException();

    public ICalculatedFieldBuilder HasCalculatedField(string name)
        => throw new System.NotImplementedException();
}