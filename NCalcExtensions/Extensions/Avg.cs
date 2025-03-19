using System.Collections;
using System.ComponentModel;

namespace NCalcExtensions.Extensions;

/// <summary>
/// Used to provide IntelliSense in Monaco editor
/// </summary>
public partial interface IFunctionPrototypes
{
    [DisplayName("avg"),Description("Emits the maximum value, ignoring nulls.")]
    object? Avg(
        [Description("The list of values")]
        IEnumerable<object?> list,
        [Description("(Optional) a string to represent the value to be evaluated")]
        string? predicate = null,
        [Description("(Optional, but must be provided if predicate is) the string to evaluate")]
        string? exprStr = null
    );
}

internal static class Avg
{
	internal static void Evaluate(FunctionArgs functionArgs)
	{
		var originalListUntyped = functionArgs.Parameters[0].Evaluate();

		if (originalListUntyped is null)
		{
			functionArgs.Result = null;
			return;
		}

		var originalList = originalListUntyped as IEnumerable ?? throw new FormatException($"First {ExtensionFunction.Avg} parameter must be an IEnumerable.");

		if (functionArgs.Parameters.Length == 1)
		{
			functionArgs.Result = originalList switch {
				null => null,
				IEnumerable<byte> list => list.Average(e=>e),
				IEnumerable<byte?> list => list.DefaultIfEmpty(null).Average(e=>e),
				IEnumerable<short> list => list.Average(e=>e),
				IEnumerable<short?> list => list.DefaultIfEmpty(null).Average(e=>e),
				IEnumerable<int> list => list.Average(),
				IEnumerable<int?> list => list.DefaultIfEmpty(null).Average(),
				IEnumerable<long> list => list.Average(),
				IEnumerable<long?> list => list.DefaultIfEmpty(null).Average(),
				IEnumerable<float> list => list.Average(),
				IEnumerable<float?> list => list.DefaultIfEmpty(null).Average(),
				IEnumerable<double> list => list.Average(),
				IEnumerable<double?> list => list.DefaultIfEmpty(null).Average(),
				IEnumerable<decimal> list => list.Average(),
				IEnumerable<decimal?> list => list.DefaultIfEmpty(null).Average(),
				IEnumerable<string?> list => list.DefaultIfEmpty(null).Average(Convert.ToDouble),
				IEnumerable<object?> list when list.All(x => x is string or null) => list.DefaultIfEmpty(null).Average(Convert.ToDouble),
				_ => throw new FormatException($"First {ExtensionFunction.Avg} parameter must be an IEnumerable of a numeric or string type if only one parameter is present.")
			};

			return;
		}

		var predicate = functionArgs.Parameters[1].Evaluate() as string
			 ?? throw new FormatException($"Second {ExtensionFunction.Avg} parameter must be a string.");

		var lambdaString = functionArgs.Parameters[2].Evaluate() as string
			 ?? throw new FormatException($"Third {ExtensionFunction.Avg} parameter must be a string.");

		var lambda = new Lambda(predicate, lambdaString, functionArgs.Parameters[0].Parameters);

		functionArgs.Result = originalList switch {
			IEnumerable<byte> list => list.Average(value => (int?)lambda.Evaluate(value)),
			IEnumerable<byte?> list => list.Average(value => (int?)lambda.Evaluate(value)),
			IEnumerable<short> list => list.Average(value => (int?)lambda.Evaluate(value)),
			IEnumerable<short?> list => list.Average(value => (int?)lambda.Evaluate(value)),
			IEnumerable<int> list => list.Average(value => (int?)lambda.Evaluate(value)),
			IEnumerable<int?> list => list.Average(value => (int?)lambda.Evaluate(value)),
			IEnumerable<long> list => list.Average(value => (long?)lambda.Evaluate(value)),
			IEnumerable<long?> list => list.Average(value => (long?)lambda.Evaluate(value)),
			IEnumerable<float> list => list.Average(value => (float?)lambda.Evaluate(value)),
			IEnumerable<float?> list => list.Average(value => (float?)lambda.Evaluate(value)),
			IEnumerable<double> list => list.Average(value => (double?)lambda.Evaluate(value)),
			IEnumerable<double?> list => list.Average(value => (double?)lambda.Evaluate(value)),
			IEnumerable<decimal> list => list.Average(value => (decimal?)lambda.Evaluate(value)),
			IEnumerable<decimal?> list => list.Average(value => (decimal?)lambda.Evaluate(value)),
			_ => throw new FormatException($"First {ExtensionFunction.Avg} parameter must be an IEnumerable of a string or numeric type when processing as a lambda.")
		};

	}
}
