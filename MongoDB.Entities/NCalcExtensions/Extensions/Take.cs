﻿using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace NCalcExtensions.Extensions;

/// <summary>
/// Used to provide IntelliSense in Monaco editor
/// </summary>
public partial interface IFunctionPrototypes
{
    [DisplayName("take"),Description("Takes a number of items from a list.\r\n\r\nNote: If a number is provided that is longer than the list, the full list is returned.")]
    List<object?> Take(
        [Description("The list to take from.")]
        IList list,
        [Description("The number of items to take.")]
        int count
    );
}

internal static class Take
{
	internal static void Evaluate(FunctionArgs functionArgs)
	{
		var list = (IList)functionArgs.Parameters[0].Evaluate();
		var numberToTake = (int)functionArgs.Parameters[1].Evaluate();
		functionArgs.Result = list.Cast<object?>().Take(numberToTake).ToList();
	}
}
