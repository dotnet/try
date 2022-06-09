using System.Text.RegularExpressions;

namespace Microsoft.TryDotNet.SimulatorGenerator;

internal static class StringExtensions
{
    public static string FixedToken(this string source)
    {
        return Regex.Replace(source, @"""token""\s*:\s*""(?<token>([^""\\]|(\\.))*)""", @"""token"": ""command-token""", RegexOptions.IgnoreCase);
    }

    public static string FixedId(this string source)
    {
        return Regex.Replace(source, @"""id""\s*:\s*""(?<id>([^""\\]|(\\.))*)""", @"""id"": ""command-id""", RegexOptions.IgnoreCase);
    }

    public static string FixedNewLine(this string source)
    {
        return Regex.Replace(source, @"\\r\\n", @"\n");
    }

    public static string FixedAssembly(this string source)
    {
        return Regex.Replace(source, @"(?<start>""assembly""\s*:\s*\{\s*""value""\s*:\s*"")(?<value>([^""\\]|(\\.))*)(?<end>""\s*\}\s*)", "${start}AABBCC${end}", RegexOptions.Multiline);
    }


    public static string Fixed(this string source)
    {
        return source.FixedId().FixedToken().FixedNewLine().FixedAssembly();
    }
}