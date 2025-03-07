namespace MongoDB.Entities;

public interface IVariableBuilder:IBuilder {
    
}

/*public interface IValueVariableBuilder:IVariableBuilder {
    public NumberVariable Variable { get; }
    public IValueVariableBuilder HasName(string name);
    public IValueVariableBuilder HasValue(double value);
}

public interface IRefVariableBuilder : IVariableBuilder {
    public ReferenceVariable Variable { get; }
    public IRefVariableBuilder HasName(string name);
    public IRefVariableBuilder ReferenceCollection(string collectionName);
    public IRefVariableBuilder WithReferenceProperty(string propertyName);
}

public interface IRefSubVariableBuilder : IVariableBuilder {
    //public ChildReferenceVariable Variable { get; }
    public IRefSubVariableBuilder HasName(string name);
    public IRefSubVariableBuilder ReferenceCollection(string collectionName);
    public IRefSubVariableBuilder WithReferenceProperty(string propertyName);
    public IRefSubVariableBuilder WithReferenceSubProperty(string propertyName);
    public IRefSubVariableBuilder IsCollection(bool isCollection);
    public IRefSubVariableBuilder WithQueryExpression(string queryExpression);
}*/