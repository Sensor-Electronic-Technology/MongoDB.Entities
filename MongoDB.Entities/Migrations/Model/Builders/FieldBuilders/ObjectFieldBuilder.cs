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

    //public ObjectFieldBuilder Builder { get; set; } = new ObjectFieldBuilder();

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
        //var builder=BuilderFactory.GetBuilder<IValueFieldBuilder>();
        var builder=new ValueFieldBuilder();
        configure(builder);
        this.Field.Fields.Add(builder.Field);
        return this;
    }

    public IObjectFieldBuilder HasSelectionField(Action<ISelectionFieldBuilder> configure) {
        //var builder = BuilderFactory.GetBuilder<ISelectionFieldBuilder>();
        var builder = new SelectionFieldBuilder();
        configure(builder);
        this.Field.Fields.Add(builder.Field);
        return this;
    }

    public IObjectFieldBuilder HasCalculatedField(Action<ICalculatedFieldBuilder> configure) {
        //var builder=BuilderFactory.GetBuilder<ICalculatedFieldBuilder>();
        var builder = new CalculatedFieldBuilder();
        configure(builder);
        this.Field.Fields.Add(builder.Field);
        return this;
    }

    public Field Build()
        => this.Field;
}