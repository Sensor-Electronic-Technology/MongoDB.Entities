using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;


[BsonDiscriminator(RootClass = true), 
 BsonKnownTypes(typeof(ValueVariable), typeof(CollectionVariable), typeof(PropertyVariable),typeof(ReferencePropertyVariable))]

public class Variable {
    public string VariableName { get; set; } = null!;
    public ValueType ValueType { get; set; }
}
/// <summary>
/// Property from the owning entity
/// </summary>
public class PropertyVariable : Variable {
    public string PropertyName { get; set; } = null!;
}
/// <summary>
/// Property from a reference collection
/// </summary>

public class ReferencePropertyVariable:PropertyVariable {
    public string DatabaseName { get; set; } = null!;
    public string CollectionName { get; set; } = null!;
    public Filter? Filter { get; set; }
}

public class ReferenceCollectionVariable:Variable{
    public string DatabaseName { get; set; } = null!;
    public string CollectionName { get; set; } = null!;
    public Filter? Filter { get; set; }
    public string Property { get; set; } = null!;
    public string CollectionProperty { get; set; } = null!;
    public Filter? SubFilter { get; set; }
}

/// <summary>
/// Collection in the owning entity
/// </summary>
public class CollectionVariable:Variable {
    public string Property { get; set; } = null!;
    public string CollectionProperty { get; set; } = null!;
    public Filter? Filter { get; set; }
}

/// <summary>
/// Static value
/// Could be false, pass, 1, 2.5, etc
/// </summary>
public class ValueVariable : Variable {
    public object Value { get; set; } = null!;
    public TypeCode TypeCode { get; set; }
}

/*public abstract class Variable {
    public string VariableName { get; set; } = string.Empty;
}

public class ValueVariable : Variable {
    
}

public class NumberVariable : ValueVariable {
    public double Value { get; set; }
}

public class DateVariable : ValueVariable {
    public string Date { get; set; }
}

public class StringVariable : ValueVariable {
    public string Value { get; set; }
}

public class BooleanVariable : ValueVariable {
    public bool Value { get; set; }
}

public class T : Variable {
    
}

public class ReferenceVariable:Variable {
    public string TypeName { get; set; } = null!;
    public string PropertyName { get; set; } = null!;
    public string QueryExpression { get; set; } = null!;
    public object? QueryCondition { get; set; }
    public ReturnType ReturnType { get; set; }
}

//AVG([Data1]) where Data1 is a property the collection ReactorLog-SubData( ReactorLog.SubData.Data1 )
public class ChildReferenceVariable:ReferenceVariable{
    public string? ChildProperty { get; set; }
}*/
