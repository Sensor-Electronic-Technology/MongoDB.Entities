﻿using System.Collections;
using System.ComponentModel;

namespace NCalcExtensions.Extensions;

/// <summary>
/// Used to provide IntelliSense in Monaco editor
/// </summary>
public partial interface IFunctionPrototypes
{
    [DisplayName("reverse"),Description("Reverses an IEnumerable.")]
    List<object?> Reverse(
        [Description("The list of items to be reversed.")]
        IList list
    );
}

internal static class Reverse
{
	internal static void Evaluate(FunctionArgs functionArgs)
	{
		var enumerable = functionArgs.Parameters[0].Evaluate() as IList
			?? throw new FormatException($"First {ExtensionFunction.Reverse} parameter must be an IEnumerable.");

		functionArgs.Result = enumerable.Cast<object?>().Reverse().ToList();
	}
}
