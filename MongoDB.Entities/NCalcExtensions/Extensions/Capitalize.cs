﻿using System.ComponentModel;
using NCalcExtensions.Exceptions;
using NCalcExtensions.Helpers;

namespace NCalcExtensions.Extensions;

/// <summary>
/// Used to provide IntelliSense in Monaco editor
/// </summary>
public partial interface IFunctionPrototypes
{
    [DisplayName("capitalize"),Description("Capitalizes a text string.")]
    string Capitalize(
        [Description("The text to capitalize.")]
        string text
    );
}

internal static class Capitalize
{
	internal static void Evaluate(FunctionArgs functionArgs)
	{
		string param1;
		try
		{
			param1 = (string)functionArgs.Parameters[0].Evaluate();
		}
		catch (Exception e) when (e is not NCalcExtensionsException or FormatException)
		{
			throw new FormatException($"{ExtensionFunction.Capitalize} function -  requires one string parameter.");
		}

		functionArgs.Result = param1.ToLowerInvariant().UpperCaseFirst();
	}
}
