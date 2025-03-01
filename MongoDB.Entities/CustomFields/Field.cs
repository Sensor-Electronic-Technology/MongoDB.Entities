using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;


[BsonDiscriminator(RootClass = true),
 BsonKnownTypes(typeof(ObjectField), typeof(ValueField), typeof(SelectionField), typeof(CalculatedField))]
public class Field {
    public string FieldName { get; set; } = string.Empty;
    public BsonType BsonType { get; set; }
    public TypeCode TypeCode { get; set; }
}

public class ObjectField : Field {
   // public string? PropertyName { get; set; }
   public List<Field> Fields { get; set; } = [];
}

public class ValueField : Field {
    public object? DefaultValue { get; set; }
    public string? UnitName { get; set; } = string.Empty;
    public string? QuantityName { get; set; } = string.Empty;
}

public class SelectionField : Field {
    public Dictionary<string,object> SelectionDictionary { get; set; } = new Dictionary<string,object>();
    public object? DefaultValue { get; set; }
}

public class CalculatedField:ValueField  {
    public string Formula { get; set; } = string.Empty;
    public List<Variable> Variables { get; set; } = [];
}