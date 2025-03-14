using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;


[BsonDiscriminator(RootClass = true),
 BsonKnownTypes(typeof(ObjectField), typeof(ValueField), typeof(SelectionField), typeof(CalculatedField))]
public class Field:IEquatable<Field> {
    public string FieldName { get; set; } = string.Empty;
    public BsonType BsonType { get; set; }
    public TypeCode TypeCode { get; set; }

    public bool Equals(Field? other) {
        if (other is null)
            return false;
        return FieldName == other.FieldName;
    }

    public override bool Equals(object? obj) {
        if (obj is null)
            return false;
        
        return Equals((Field)obj);
    }

    public override int GetHashCode()
        => HashCode.Combine(FieldName);
}

public class ObjectField : Field {
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
    public string Expression { get; set; } = string.Empty;
    public List<Variable> Variables { get; set; } = [];
}