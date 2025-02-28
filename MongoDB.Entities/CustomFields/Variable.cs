using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

//MAX([A])  AVG([Tma1Temp])
/*[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(typeof(ReferenceVariable), typeof(ReferenceSubVariable), typeof(ReferenceSubCollectionVariable))]*/
public abstract class Variable {
    public string VariableName { get; set; } = string.Empty;
}

public class ValueVariable : Variable {
    public double Value { get; set; }
}

//AVG([Tma1Temp]) where Tma1Temp is a property the collection ReactorLog( ReactorLog.Tma1Temp )
public class ReferenceVariable:Variable {
    public string? ReferenceCollection { get; set; }
    public string? ReferenceProperty { get; set; }  
}

//AVG([Data1]) where Data1 is a property the collection ReactorLog-SubData( ReactorLog.SubData.Data1 )
public class ReferenceSubVariable:ReferenceVariable{
    public string? ReferenceSubProperty { get; set; }
    public bool IsCollection { get; set; }
    public string? QueryExpression { get; set; }
}

/*public class ReferenceSubCollectionVariable:ReferenceSubVariable{
    public string? CollectionFilter { get; set; }
}*/