using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace MongoDB.Entities;


public class ObjectFieldBuilder : IObjectFieldBuilder {
    public ObjectField Field { get; set; } = new() {
        BsonType = BsonType.Document,
        TypeCode = TypeCode.Object,
        Fields = []
    };

    public IObjectFieldBuilder WithName(string name) {
        this.Field.FieldName = name;
        return this;
    }

    public IObjectFieldBuilder HasObjectField(Action<IObjectFieldBuilder> configure) {
        var builder=new ObjectFieldBuilder();
        configure(builder);
        this.Field.Fields.Add(builder.Field);
        return this;
    }

    public IObjectFieldBuilder HasValueField(Action<IValueFieldBuilder> configure) {

        return this;
    }

    public IObjectFieldBuilder HasSelectionField(Action<ISelectionFieldBuilder> configure)
        => throw new NotImplementedException();

    public IObjectFieldBuilder HasCalculatedField(Action<ICalculatedFieldBuilder> configure)
        => throw new NotImplementedException();

    public ObjectField Build()
        => throw new NotImplementedException();
}