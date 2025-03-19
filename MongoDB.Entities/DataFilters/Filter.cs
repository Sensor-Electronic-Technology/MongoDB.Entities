using System.Collections.Generic;

namespace MongoDB.Entities;

public class Filter {
    public string FieldName { get; set; } = string.Empty;
    public string CompareOperator { get; set; } = ComparisonOperator.Equal.Value;
    public string FilterLogicalOperator { get; set; } = LogicalOperator.And.Value;
    public object Value { get; set; } = null!;
    public ICollection<Filter>? Filters { get; set; } = [];
    public override string ToString() {
        return "e=>"+BuildFilterString(this);
    }
    internal static string BuildFilterString(Filter filter) {
        if (filter.Filters==null || filter.Filters.Count == 0) {
            return $"e.{filter.FieldName} {filter.CompareOperator} {filter.Value})";
        }
        List<string> whereClauses = [
            $"(e.{filter.FieldName} {filter.CompareOperator} {filter.Value}"
        ];
        foreach (var f in filter.Filters) {
            whereClauses.Add(BuildFilterString(f));
        }
        return string.Join($" {filter.FilterLogicalOperator} ", whereClauses);
    }
}

public enum FilterValueType {
    EntityId,
    Entity,
    Value
}