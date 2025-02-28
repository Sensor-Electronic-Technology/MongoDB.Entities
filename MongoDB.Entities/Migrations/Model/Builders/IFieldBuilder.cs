using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace MongoDB.Entities;





public interface IObjectFieldBuilder {
    protected ObjectField Field { get; }
    public static virtual IObjectFieldBuilder Create()=>throw new NotImplementedException();
    public IObjectFieldBuilder WithName(string name);
    public IObjectFieldBuilder HasObjectField(Action<IObjectFieldBuilder> configure);
    public IObjectFieldBuilder HasValueField(Action<IValueFieldBuilder> configure);
    public IObjectFieldBuilder HasSelectionField(Action<ISelectionFieldBuilder> configure);
    public IObjectFieldBuilder HasCalculatedField(Action<ICalculatedFieldBuilder> configure);
    public ObjectField Build();
    
}

public interface IValueFieldBuilder {
    public IValueFieldBuilder(){}
    /*public IFieldBuilder WithClrType<TClr>();
    public IFieldBuilder WithBsonType(BsonType bsonType);*/
    public IValueFieldBuilder WithName(string name);
    public IValueFieldBuilder HasDefaultValue(object? defaultValue,string UnitName,string QuantityName);
    public static virtual IObjectFieldBuilder Create()=>throw new NotImplementedException();
}

public interface ISelectionFieldBuilder {
    /*public IFieldBuilder WithClrType<TClr>();
    public IFieldBuilder WithBsonType(BsonType bsonType);*/
    public ISelectionFieldBuilder WithSelectionItems(IEnumerable<string> Items);
    public static virtual IObjectFieldBuilder Create()=>throw new NotImplementedException();
}

public interface ICalculatedFieldBuilder {
    /*public IFieldBuilder WithClrType<TClr>();
    public IFieldBuilder WithBsonType(BsonType bsonType);*/
    public ICalculatedFieldBuilder WithVariable(string name);
    public ICalculatedFieldBuilder WithExpression(string expression);
    public static virtual IObjectFieldBuilder Create()=>throw new NotImplementedException();
    
}