using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace MongoDB.Entities;

public interface IEntityBuilder {
    public List<Field> Fields { get; set; }
    /*public IEntityBuilder HasObjectField(Action<IObjectFieldBuilder> configure);
    public IEntityBuilder HasValueField(Action<IValueFieldBuilder> configure);
    public IEntityBuilder HasSelectionField(Action<ISelectionFieldBuilder> configure);
    public IEntityBuilder HasCalculatedField(Action<ICalculatedFieldBuilder> configure);*/
    //public BsonDocument Build();
    
}

public class EntityBuilder:IEntityBuilder {
    public List<Field> Fields { get; set; } = [];
    /*public IEntityBuilder HasObjectField(Action<IObjectFieldBuilder> configure) {
        var builder=BuilderFactory.GetBuilder<IObjectFieldBuilder>();
        configure(builder);
        this.Fields.Add(builder.Field);

        return this;
    }

    public IEntityBuilder HasValueField(Action<IValueFieldBuilder> configure){
        var builder=BuilderFactory.GetBuilder<IValueFieldBuilder>();
        configure(builder);
        this.Fields.Add(builder.Field);

        return this;
    }

    public IEntityBuilder HasSelectionField(Action<ISelectionFieldBuilder> configure){
        var builder=BuilderFactory.GetBuilder<ISelectionFieldBuilder>();
        configure(builder);
        this.Fields.Add(builder.Field);

        return this;
    }

    public IEntityBuilder HasCalculatedField(Action<ICalculatedFieldBuilder> configure){
        var builder=BuilderFactory.GetBuilder<ICalculatedFieldBuilder>();
        configure(builder);
        this.Fields.Add(builder.Field);

        return this;
    }*/
    
}