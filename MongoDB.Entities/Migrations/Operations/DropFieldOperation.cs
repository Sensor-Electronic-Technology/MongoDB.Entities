﻿namespace MongoDB.Entities;

public class DropFieldOperation:IFieldOperation {
    public string CollectionName { get; set; }= null!;
    public string PropertyName { get; set; }= null!;
    public Field Field { get; set; } = null!;
    public bool IsDestructive { get; set; } = true;
}