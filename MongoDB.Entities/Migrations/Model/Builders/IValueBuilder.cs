namespace MongoDB.Entities;

public interface IVariableBuilder {
    public IVariableBuilder HasName(string name);
}

public interface IValueVariableBuilder:IVariableBuilder {
    public IValueVariableBuilder HasValue(object value);
}

public interface IRefVariableBuilder : IVariableBuilder {
    public IRefVariableBuilder ReferenceCollection(string collectionName);
    public IRefVariableBuilder WithReferenceProperty(string propertyName);
}

public interface IRefSubVariableBuilder : IRefVariableBuilder {
    public IRefSubVariableBuilder WithReferenceSubProperty(string propertyName);
    public IRefSubVariableBuilder IsCollection(bool isCollection);
    public IRefSubVariableBuilder WithQueryExpression(string queryExpression);
}