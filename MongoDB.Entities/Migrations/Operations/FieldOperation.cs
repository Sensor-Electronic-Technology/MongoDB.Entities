using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

[BsonDiscriminator(RootClass = true),
 BsonKnownTypes(typeof(DropFieldOperation), typeof(AddFieldOperation), typeof(AlterFieldOperation))]
public class FieldOperation {
    public bool IsDestructive { get; set; }
}

public class DropFieldOperation:FieldOperation {
    public Field Field { get; set; } = null!;
}

public class AddFieldOperation : FieldOperation {
    public Field Field { get; set; } = null!;
}

public class AlterFieldOperation:FieldOperation {
    public Field Field { get; set; } = null!;
    //public AddFieldOperation OldField { get; set; } = null!;
    public Field OldField { get; set; } = null!;
}