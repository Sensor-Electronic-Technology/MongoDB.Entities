﻿namespace NCalcExtensions.Helpers;
internal static class StringExtensions
{
	internal static string UpperCaseFirst(this string s)
		=> s == null
			? throw new ArgumentNullException(nameof(s))
			: $"{s.Substring(0, 1).ToUpperInvariant()}{s.Substring(1)}";
}
