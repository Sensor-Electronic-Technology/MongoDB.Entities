namespace MongoDB.Entities;

public class ReferenceVariableBuilder:IRefVariableBuilder {
    public ReferenceVariable Variable { get; }

    public IRefVariableBuilder HasName(string name) {
        this.Variable.VariableName = name;
        return this;
    }

    public IRefVariableBuilder ReferenceCollection(string collectionName) {
        this.Variable.ReferenceCollection = collectionName;
        return this;
    }

    public IRefVariableBuilder WithReferenceProperty(string propertyName) {
        this.Variable.ReferenceProperty = propertyName;
        return this;
    }
}