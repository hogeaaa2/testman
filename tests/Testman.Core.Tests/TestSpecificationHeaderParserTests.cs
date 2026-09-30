using Testman.Core.Specifications;

namespace Testman.Core.Tests;

public sealed class TestSpecificationHeaderParserTests
{
    [Fact]
    public void Parse_returns_version_when_first_line_is_valid()
    {
        const string source = "Testman-Format-Version: 1\n\n# Login tests";

        var result = TestSpecificationHeaderParser.Parse(source);

        Assert.True(result.IsValid);
        Assert.Equal(1, result.FormatVersion);
        Assert.Null(result.Diagnostic);
    }

    [Fact]
    public void Parse_reports_missing_version_when_first_line_is_not_a_version()
    {
        const string source = "# Login tests";

        var result = TestSpecificationHeaderParser.Parse(source);

        Assert.False(result.IsValid);
        Assert.Null(result.FormatVersion);
        Assert.Contains("missing", result.Diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Testman-Format-Version: current")]
    [InlineData("Testman-Format-Version: 0")]
    [InlineData("Testman-Format-Version: -1")]
    public void Parse_reports_invalid_version_when_value_is_not_a_positive_integer(string firstLine)
    {
        var result = TestSpecificationHeaderParser.Parse(firstLine);

        Assert.False(result.IsValid);
        Assert.Null(result.FormatVersion);
        Assert.Contains("positive integer", result.Diagnostic, StringComparison.OrdinalIgnoreCase);
    }
}
