using Testman.Core.Specifications;

namespace Testman.Core.Tests;

public sealed class TestSpecificationParserTests
{
    [Fact]
    public void Parse_returns_a_valid_title_block()
    {
        var result = TestSpecificationParser.Parse(ValidSpecification("Login tests", "TC-1"), "login.md");

        var title = Assert.Single(result.Titles);
        Assert.Equal("Login tests", title.Name);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Parse_returns_each_valid_title_block()
    {
        var source = "Testman-Format-Version: 1\n\n"
            + ValidTitleBlock("Login tests", "TC-1")
            + "\n"
            + ValidTitleBlock("Logout tests", "TC-2");

        var result = TestSpecificationParser.Parse(source, "authentication.md");

        Assert.Equal(["Login tests", "Logout tests"], result.Titles.Select(title => title.Name));
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData("## Overview", "Overview")]
    [InlineData("## Preconditions", "Preconditions")]
    [InlineData("## Common steps", "Common steps")]
    public void Parse_omits_a_title_when_a_required_section_is_missing(string section, string expectedReason)
    {
        var source = ValidSpecification("Login tests", "TC-1").Replace($"{section}\n\nContent\n\n", string.Empty);

        var result = TestSpecificationParser.Parse(source, "login.md");

        Assert.Empty(result.Titles);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("login.md", diagnostic.SourcePath);
        Assert.Contains(expectedReason, diagnostic.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_omits_a_title_when_required_sections_are_out_of_order()
    {
        var source = ValidSpecification("Login tests", "TC-1")
            .Replace("## Overview\n\nContent\n\n## Preconditions", "## Preconditions\n\nContent\n\n## Overview");

        var result = TestSpecificationParser.Parse(source, "login.md");

        Assert.Empty(result.Titles);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Reason.Contains("order", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Overview")]
    [InlineData("Preconditions")]
    [InlineData("Common steps")]
    public void Parse_omits_a_title_when_a_required_section_is_empty(string section)
    {
        var source = ValidSpecification("Login tests", "TC-1")
            .Replace($"## {section}\n\nContent", $"## {section}\n");

        var result = TestSpecificationParser.Parse(source, "login.md");

        Assert.Empty(result.Titles);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Reason.Contains(section, StringComparison.Ordinal)
            && diagnostic.Reason.Contains("empty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_omits_a_title_when_the_test_case_table_is_missing()
    {
        var source = ValidSpecification("Login tests", "TC-1").Replace(ValidTable("TC-1"), string.Empty);

        var result = TestSpecificationParser.Parse(source, "login.md");

        Assert.Empty(result.Titles);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Reason.Contains("exactly one", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_omits_a_title_when_it_has_multiple_tables()
    {
        var source = ValidSpecification("Login tests", "TC-1") + "\n\n" + ValidTable("TC-2");

        var result = TestSpecificationParser.Parse(source, "login.md");

        Assert.Empty(result.Titles);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Reason.Contains("exactly one", StringComparison.OrdinalIgnoreCase));
    }

    private static string ValidSpecification(string title, string id) =>
        $"Testman-Format-Version: 1\n\n{ValidTitleBlock(title, id)}";

    private static string ValidTitleBlock(string title, string id) =>
        $$"""
        # {{title}}

        ## Overview

        Content

        ## Preconditions

        Content

        ## Common steps

        Content

        {{ValidTable(id)}}
        """;

    private static string ValidTable(string id) =>
        $$"""
        | ID | Major item | Middle item | Minor item | Steps | Expected result |
        |---|---|---|---|---|---|
        | {{id}} | - | - | Login | - | Dashboard is displayed. |
        """;
}
