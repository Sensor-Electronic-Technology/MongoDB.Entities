namespace MongoDB.Entities;

public class ValueVariableBuilder:IValueVariableBuilder {
    public NumberVariable Variable { get; }

    public IValueVariableBuilder HasName(string name) {
        this.Variable.VariableName = name;
        return this;
    }

    public IValueVariableBuilder HasValue(double value) {
        this.Variable.Value = value;
        return this;
    }
}