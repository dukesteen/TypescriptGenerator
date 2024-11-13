namespace TypescriptGenerator.Console.ImmediateApisTsGen.Helpers;

internal static class StringHelpers
{
	public static string ToCamelCase(this string str)
	{
		return string.IsNullOrEmpty(str) ? str : char.ToLowerInvariant(str[0]) + str[1..];
	}
}
