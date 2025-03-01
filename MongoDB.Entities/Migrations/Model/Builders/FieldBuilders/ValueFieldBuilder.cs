using System;
using MongoDB.Bson;

namespace MongoDB.Entities;

public class ValueFieldBuilder:IValueFieldBuilder {
    public ValueField Field { get; } = new();
    //public IValueFieldBuilder Builder { get; set; } = new ValueFieldBuilder();

    public IValueFieldBuilder WithName(string name) {
        this.Field.FieldName = name;
        return this;
    }

    public IValueFieldBuilder HasDefaultValue(object? defaultValue, string UnitName, string QuantityName) {
        this.Field.UnitName = UnitName;
        this.Field.QuantityName = QuantityName;
        this.Field.DefaultValue = defaultValue;
        this.Field.TypeCode=Type.GetTypeCode(defaultValue?.GetType());
        this.Field.BsonType = BsonValue.Create(defaultValue).BsonType;
        return this;
    }

    public Field Build()
        => this.Field;
}