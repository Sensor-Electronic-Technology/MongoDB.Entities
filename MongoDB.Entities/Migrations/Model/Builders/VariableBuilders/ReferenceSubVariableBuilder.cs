namespace MongoDB.Entities;

public class ReferenceSubVariableBuilder:IRefSubVariableBuilder {
    public ReferenceSubVariable Variable { get; }

    public IRefSubVariableBuilder WithReferenceSubProperty(string propertyName) {
        this.Variable.ReferenceSubProperty = propertyName;
        return this;
    }

    public IRefSubVariableBuilder IsCollection(bool isCollection) {
        this.Variable.IsCollection = isCollection;
        return this;
    }

    public IRefSubVariableBuilder WithQueryExpression(string queryExpression) {
        this.Variable.QueryExpression = queryExpression;
        return this;
    }

    public IRefSubVariableBuilder HasName(string name) {
        this.Variable.VariableName = name;
        return this;
    }

    public IRefSubVariableBuilder ReferenceCollection(string collectionName) {
        this.Variable.ReferenceCollection = collectionName;
        return this;
    }

    public IRefSubVariableBuilder WithReferenceProperty(string propertyName) {
        this.Variable.ReferenceProperty = propertyName;
        return this;
    }
}