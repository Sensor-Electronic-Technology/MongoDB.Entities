using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;


[BsonDiscriminator(RootClass = true),
 BsonKnownTypes(typeof(ReferenceVariable), typeof(ReferenceSubVariable), typeof(ReferenceSubVariable))]
public abstract class Variable {
    public string VariableName { get; set; } = string.Empty;
}

public class NumberVariable : Variable {
    public double Value { get; set; }
}

public class DateVariable : Variable {
    public string Date { get; set; }
}

public class StringVariable : Variable {
    public string Value { get; set; }
}

public class BooleanVariable : Variable {
    public bool Value { get; set; }
}

public class ReferenceVariable:Variable {
    public string? ReferenceCollection { get; set; }
    public string? ReferenceProperty { get; set; }  
    public string? QueryExpression { get; set; }
    public bool IsCollection { get; set; }
}

//AVG([Data1]) where Data1 is a property the collection ReactorLog-SubData( ReactorLog.SubData.Data1 )
public class ReferenceSubVariable:ReferenceVariable{
    public string? SubProperty { get; set; }
}