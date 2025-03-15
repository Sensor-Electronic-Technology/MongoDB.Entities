namespace ConsoleTesting;

public enum VariableType {
    NUMBER,
    STRING,
    BOOLEAN,
    DATE,
    LIST_NUMBER,
    LIST_STRING,
    LIST_BOOLEAN,
    LIST_DATE
}

public class Variable {
    public string VariableName { get; set; } = null!;
    public VariableType VariableType { get; set; }
}

/**
 * Object.Property
 */

public class PropertyVariable:Variable {
    public string PropertyName { get; set; } = null!;
}

/**
 * 
 */

public class ReferenceCollectionVariable:Variable{
    public string DatabaseName { get; set; } = null!;
    public string CollectionName { get; set; } = null!;
    //public Filter? Filter { get; set; }
    public string Property { get; set; } = null!;
    public string CollectionProperty { get; set; } = null!;
    //public Filter? SubFilter { get; set; }
}

/**
 * Object.CollectionProperty[Filter].Property
 */

public class CollectionPropertyVariable : Variable {
    public string Property { get; set; } = null!;
    public string CollectionProperty { get; set; } = null!;
    //Filter
}


