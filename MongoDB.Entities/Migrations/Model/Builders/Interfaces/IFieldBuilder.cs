using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace MongoDB.Entities;


public interface IFieldBuilder:IBuilder {
    public Field Build();
}



public interface IObjectFieldBuilder:IFieldBuilder {
    public ObjectField Field { get;  }
    //protected ObjectFieldBuilder Builder { get; set; }
    public IObjectFieldBuilder WithName(string name);
    public IObjectFieldBuilder HasObjectField(Action<IObjectFieldBuilder> configure);
    public IObjectFieldBuilder HasValueField(Action<IValueFieldBuilder> configure);
    public IObjectFieldBuilder HasSelectionField(Action<ISelectionFieldBuilder> configure);
    public IObjectFieldBuilder HasCalculatedField(Action<ICalculatedFieldBuilder> configure);
    
}

public interface IValueFieldBuilder:IFieldBuilder {
    public ValueField Field { get;  }
    //protected IValueFieldBuilder Builder { get; set; }
    /*public IFieldBuilder WithClrType<TClr>();
    public IFieldBuilder WithBsonType(BsonType bsonType);*/
    
    public IValueFieldBuilder WithName(string name);
    public IValueFieldBuilder HasDefaultValue(object? defaultValue,string UnitName,string QuantityName);
}

public interface ISelectionFieldBuilder:IFieldBuilder{
    /*public IFieldBuilder WithClrType<TClr>();
    public IFieldBuilder WithBsonType(BsonType bsonType);*/
    public SelectionField Field { get;  }
    //protected ISelectionFieldBuilder Builder { get; set; }
    public ISelectionFieldBuilder WithName(string name);
    public ISelectionFieldBuilder WithSelectionItems<TValue>(Dictionary<string,TValue> selectionItems);
    public ISelectionFieldBuilder HasSelectionItem<TValue>(string key,TValue value);
}

public interface ICalculatedFieldBuilder:IFieldBuilder {
    /*public IFieldBuilder WithClrType<TClr>();
    public IFieldBuilder WithBsonType(BsonType bsonType);*/
    public CalculatedField Field { get;  }
    //protected ICalculatedFieldBuilder Builder { get; set; }
    public ICalculatedFieldBuilder WithVariable(Action<IVariableBuilder> configure);
}