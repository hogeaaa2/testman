using System.Globalization;

namespace Testman.Core.Specifications;

public static class TestSpecificationHeaderParser
{
    private const string Prefix = "Testman-Format-Version: ";

    public static TestSpecificationHeaderParseResult Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var lineEnd = source.AsSpan().IndexOfAny('\r', '\n');
        var firstLine = lineEnd >= 0 ? source.AsSpan(0, lineEnd) : source.AsSpan();

        if (!firstLine.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return TestSpecificationHeaderParseResult.Invalid("Format version is missing from the first line.");
        }

        var value = firstLine[Prefix.Length..];
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var version) || version <= 0)
        {
            return TestSpecificationHeaderParseResult.Invalid("Format version must be a positive integer.");
        }

        return TestSpecificationHeaderParseResult.Valid(version);
    }
}

public sealed record TestSpecificationHeaderParseResult(
    bool IsValid,
    int? FormatVersion,
    string? Diagnostic)
{
    internal static TestSpecificationHeaderParseResult Valid(int version) => new(true, version, null);

    internal static TestSpecificationHeaderParseResult Invalid(string diagnostic) => new(false, null, diagnostic);
}
