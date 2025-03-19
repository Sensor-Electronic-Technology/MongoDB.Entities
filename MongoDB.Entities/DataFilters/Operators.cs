using Ardalis.SmartEnum;

namespace MongoDB.Entities;

public class ComparisonOperator : SmartEnum<ComparisonOperator, string> {
    public static readonly ComparisonOperator Equal = new("Equal", "==","eq");
    public static readonly ComparisonOperator NotEqual = new("NotEquals", "!=","ne");
    public static readonly ComparisonOperator LessThan = new("LessThan", "<","lt");
    public static readonly ComparisonOperator LessThanOrEqual = new("LessThanOrEqual", "<=", "le");
    public static readonly ComparisonOperator GreaterThan = new("GreaterThan", ">", "gt");
    public static readonly ComparisonOperator GreaterThanOrEqual = new("GreaterThanOrEqual", ">=", "ge");
    public static readonly ComparisonOperator In = new("In", "in", "in");
    public static readonly ComparisonOperator NotIn = new("NotIn", "not in", "nin");
    public static readonly ComparisonOperator StartsWith = new("StartsWith","StartsWith","startswith");
    public static readonly ComparisonOperator EndsWith = new("EndsWith","EndsWith","endswith");
    public static readonly ComparisonOperator Contains = new("Contains","Contains","contains");
    public static readonly ComparisonOperator DoesNotContain = new("DoesNotContain","DoesNotContain","DoesNotContain");
    
    public string ODataOperator { get; }

    public ComparisonOperator(string name, string value, string oDataOp) : base(name, value) {
        ODataOperator=oDataOp;
    }
}

public class LogicalOperator : SmartEnum<LogicalOperator, string> {
    public static readonly LogicalOperator And = new("And","&&","and");
    public static readonly LogicalOperator Or = new("Or", "||","or");
    public string ODataOperator { get; }

    public LogicalOperator(string name, string value, string oDataOp) : base(name, value) {
        ODataOperator=oDataOp;
    }
}