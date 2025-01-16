namespace Api.Core.Extensions;

/// <summary>
/// A class that converts strings to JSON and back
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Makes sure that a string can be used as a JSON property by converting certain characters to placeholders.
    /// </summary>
    /// <param name="input">The input string to convert.</param>
    /// <returns>A string that can safely be used as a JSON property.</returns>
    public static string MakeJsonPropertyName(this string input)
    {
        return input.Replace("-", "__h__").Replace(" ", "__s__").Replace(":", "__c__").Replace("(", "__bl__").Replace(")", "__br__").Replace(".", "__d__").Replace(",", "__co__");
    }

    /// <summary>
    /// Convert a string that was created by <see cref="MakeJsonPropertyName"/> back into it's original form.
    /// </summary>
    /// <param name="input">The input string that was converted by <see cref="MakeJsonPropertyName"/>.</param>
    /// <returns>The original string.</returns>
    public static string UnmakeJsonPropertyName(this string input)
    {
        return input.Replace("__h__", "-").Replace("__s__", " ").Replace("__c__", ":").Replace("__bl__", "(").Replace("__br__", ")").Replace("__d__", ".").Replace("__co__", ",");
    }
}