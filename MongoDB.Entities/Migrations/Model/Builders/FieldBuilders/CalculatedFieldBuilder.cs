using System;

namespace MongoDB.Entities;

public class CalculatedFieldBuilder:ICalculatedFieldBuilder {
    public CalculatedField Field { get; } = new();
    //public ICalculatedFieldBuilder Builder { get; set; }

    public ICalculatedFieldBuilder WithName(string name) {
        this.Field.FieldName = name;
        return this;
    }

    public ICalculatedFieldBuilder WithVariable(Action<IVariableBuilder> configure) {
        return this;
    }
    public Field Build()=>this.Field;
}