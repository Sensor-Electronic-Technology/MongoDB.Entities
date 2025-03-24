using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Extensions;

namespace NCalcExtensions.Extensions;

/// <summary>
/// Used to provide IntelliSense in Monaco editor
/// </summary>
public partial interface IFunctionPrototypes
{
    [DisplayName("median"),Description("Emits the maximum value, ignoring nulls.")]
    object? Median(
        [Description("The list of values")]
        IEnumerable<object?> list,
        [Description("(Optional) a string to represent the value to be evaluated")]
        string? predicate = null,
        [Description("(Optional, but must be provided if predicate is) the string to evaluate")]
        string? exprStr = null
    );
}

internal static class Median
{
	internal static void Evaluate(FunctionArgs functionArgs) {
		var originalListUntyped = functionArgs.Parameters[0].Evaluate();

		if (originalListUntyped is null) {
			functionArgs.Result = null;
			return;
		}

		var originalList = originalListUntyped as IEnumerable?? throw new FormatException($"First {ExtensionFunction.Median} parameter must be an IEnumerable.");
		if (functionArgs.Parameters.Length == 1) {

			functionArgs.Result = originalList switch
			{
				null => null,
				IEnumerable<byte> list => list.Median(e=>e),
				IEnumerable<byte?> list => list.DefaultIfEmpty(null).Median(e=>e),
				IEnumerable<short> list => list.Median(e=>e),
				IEnumerable<short?> list => list.DefaultIfEmpty(null).Median(e=>e),
				IEnumerable<int> list => list.Median(),
				IEnumerable<int?> list => list.DefaultIfEmpty(null).Median(),
				IEnumerable<long> list => list.Median(),
				IEnumerable<long?> list => list.DefaultIfEmpty(null).Median(),
				IEnumerable<float> list => list.Median(),
				IEnumerable<float?> list => list.DefaultIfEmpty(null).Median(),
				IEnumerable<double> list => list.Median(),
				IEnumerable<double?> list => list.DefaultIfEmpty(null).Median(),
				IEnumerable<decimal> list => list.Median(),
				IEnumerable<decimal?> list => list.DefaultIfEmpty(null).Median(),
				IEnumerable<string?> list => list.DefaultIfEmpty(null).Median(Convert.ToDouble),
				IEnumerable<object?> list when list.All(x => x is string or null) => list.DefaultIfEmpty(null).Median(Convert.ToDouble),
				_ => throw new FormatException($"First {ExtensionFunction.Avg} parameter must be an IEnumerable of a numeric or string type if only one parameter is present.")
			};
			return;
		}
		
		var predicate = functionArgs.Parameters[1].Evaluate() as string
			 ?? throw new FormatException($"Second {ExtensionFunction.Median} parameter must be a string.");

		var lambdaString = functionArgs.Parameters[2].Evaluate() as string
			 ?? throw new FormatException($"Third {ExtensionFunction.Median} parameter must be a string.");

		var lambda = new Lambda(predicate, lambdaString, functionArgs.Parameters[0].Parameters);
		
		functionArgs.Result = originalList switch {
			IEnumerable<byte> list => list.Median(value => (int?)lambda.Evaluate(value)),
			IEnumerable<byte?> list => list.Median(value => (int?)lambda.Evaluate(value)),
			IEnumerable<short> list => list.Median(value => (int?)lambda.Evaluate(value)),
			IEnumerable<short?> list => list.Median(value => (int?)lambda.Evaluate(value)),
			IEnumerable<int> list => list.Median(value => (int?)lambda.Evaluate(value)),
			IEnumerable<int?> list => list.Median(value => (int?)lambda.Evaluate(value)),
			IEnumerable<long> list => list.Median(value => (long?)lambda.Evaluate(value)),
			IEnumerable<long?> list => list.Median(value => (long?)lambda.Evaluate(value)),
			IEnumerable<float> list => list.Median(value => (float?)lambda.Evaluate(value)),
			IEnumerable<float?> list => list.Median(value => (float?)lambda.Evaluate(value)),
			IEnumerable<double> list => list.Median(value => (double?)lambda.Evaluate(value)),
			IEnumerable<double?> list => list.Median(value => (double?)lambda.Evaluate(value)),
			IEnumerable<decimal> list => list.Median(value => (decimal?)lambda.Evaluate(value)),
			IEnumerable<decimal?> list => list.Median(value => (decimal?)lambda.Evaluate(value)),
			_ => throw new FormatException($"First {ExtensionFunction.Avg} parameter must be an IEnumerable of a string or numeric type when processing as a lambda.")
		};
	}

	private static double GetMedian(IEnumerable<object?> list) {
		var sorted = list.Cast<double>().OrderBy(x => x).ToList();
		var count = sorted.Count;
		if (count % 2 == 0) {
			return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
		}
		return sorted[count / 2];
	}
}