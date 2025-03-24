using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

[BsonDiscriminator(RootClass = true), 
 BsonKnownTypes(typeof(ValueVariable), 
     typeof(CollectionPropertyVariable), 
     typeof(PropertyVariable),
     typeof(RefPropertyVariable),
     typeof(RefCollectionPropertyVariable),
     typeof(EmbeddedPropertyVariable))]
public class Variable {
    public string VariableName { get; set; } = null!;
    public DataType DataType { get; set; }
}
/// <summary>
/// Property from the owning entity
/// Object.Property
/// </summary>
public class PropertyVariable : Variable {
    public string Property { get; set; } = null!;
}

/// <summary>
///
/// Object.OwningProperty.Property
/// </summary>

public class EmbeddedPropertyVariable : PropertyVariable {
    //public string OwningPropertyName { get; set; } = null!;
    public IList<string> EmbeddedObjectProperties { get; set; } = null!;
    public string EmbeddedProperty { get; set; } = null!;
    
}

/// <summary>
/// Property from a reference collection
/// Database.Collection.Where(Filter).Property
/// </summary>

public class RefPropertyVariable:PropertyVariable {
    public string DatabaseName { get; set; } = null!;
    public string CollectionName { get; set; } = null!;
    public Filter? Filter { get; set; }
}

/// <summary>
/// Property from a reference collection with output of list type
/// Database.Collection.Where(Filter).Collection(Filter).Property
/// </summary>
/// 
public class RefCollectionPropertyVariable:PropertyVariable{
    public string DatabaseName { get; set; } = null!;
    public string CollectionName { get; set; } = null!;
    public Filter? Filter { get; set; }
    public string CollectionProperty { get; set; } = null!;
    public Filter? SubFilter { get; set; }
}

/// <summary>
/// Collection in the owning entity
/// Object.Collection.Where(Filter).Property
/// </summary>
public class CollectionPropertyVariable:PropertyVariable {
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


