using Ardalis.SmartEnum;

namespace MongoDB.Entities;

public class FilterOperator : SmartEnum<FilterOperator, string> {
    public static readonly FilterOperator Equal = new("Equal", "==","eq");
    public static readonly FilterOperator NotEqual = new("NotEquals", "!=","ne");
    public static readonly FilterOperator LessThan = new("LessThan", "<","lt");
    public static readonly FilterOperator LessThanOrEqual = new("LessThanOrEqual", "<=", "le");
    public static readonly FilterOperator GreaterThan = new("GreaterThan", ">", "gt");
    public static readonly FilterOperator GreaterThanOrEqual = new("GreaterThanOrEqual", ">=", "ge");
    public static readonly FilterOperator In = new("In", "in", "in");
    public static readonly FilterOperator NotIn = new("NotIn", "not in", "nin");
    public static readonly FilterOperator StartsWith = new("StartsWith","StartsWith","startswith");
    public static readonly FilterOperator EndsWith = new("EndsWith","EndsWith","endswith");
    public static readonly FilterOperator Contains = new("Contains","Contains","contains");
    public static readonly FilterOperator DoesNotContain = new("DoesNotContain","DoesNotContain","DoesNotContain");
    
    public string ODataOperator { get; }

    public FilterOperator(string name, string value, string oDataOp) : base(name, value) {
        this.ODataOperator=oDataOp;
    }
}

public class LogicalFilterOperator : SmartEnum<LogicalFilterOperator, string> {
    public static readonly LogicalFilterOperator And = new("And","&&","and");
    public static readonly LogicalFilterOperator Or = new("Or", "||","or");
    public string ODataOperator { get; }

    public LogicalFilterOperator(string name, string value, string oDataOp) : base(name, value) {
        this.ODataOperator=oDataOp;
    }
}