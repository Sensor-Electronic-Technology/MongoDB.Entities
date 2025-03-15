using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Entities;

public class Filter {
    public string FieldName { get; set; } = string.Empty;
    public ComparisonOperator ComparisonOperator { get; set; } 
    public LogicalOperator LogicalOperator { get; set; } 
    public object Value { get; set; } = null!;
    public ICollection<Filter>? Filters { get; set; } = [];

    public override string ToString() {
        return "e=>"+BuildFilterString(this);
    }
    internal static string BuildFilterString(Filter filter) {
        if (filter.Filters==null || filter.Filters.Count == 0) {
            return $"e.{filter.FieldName} {filter.ComparisonOperator.Value} {filter.Value})";
        }
        List<string> whereClauses = [
            $"(e.{filter.FieldName} {filter.ComparisonOperator.Value} {filter.Value}"
        ];
        foreach (var f in filter.Filters) {
            whereClauses.Add(BuildFilterString(f));
        }
        return string.Join($" {filter.LogicalOperator.Value} ", whereClauses);
    }
}

public enum FilterValueType {
    EntityId,
    Entity,
    Value
}