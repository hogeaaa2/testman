using Testman.Core.Specifications;

namespace Testman.Core.Tests;

public sealed class TestSpecificationParserTests
{
    [Fact]
    public void Parse_returns_a_valid_title_block()
    {
        var result = TestSpecificationParser.Parse(ValidSpecification("Login tests", "TC-1"), "login.md");

        var title = Assert.Single(result.Titles);
        Assert.Equal(1, result.FormatVersion);
        Assert.Equal("Login tests", title.Name);
        var testCase = Assert.Single(title.TestCases);
        Assert.Equal("TC-1", testCase.Id);
        Assert.Equal("-", testCase.MajorItem);
        Assert.Equal("-", testCase.MiddleItem);
        Assert.Equal("Login", testCase.MinorItem);
        Assert.Equal("-", testCase.Steps);
        Assert.Equal("Dashboard is displayed.", testCase.ExpectedResult);
        Assert.True(testCase.CanRegisterResult);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Parse_preserves_required_section_markdown()
    {
        const string source = """
            Testman-Format-Version: 1

            # Login tests

            ## Overview

            Verify **login** behavior.

            ## Preconditions

            - User exists.
            - User is active.

            ## Common steps

            1. Open the page.
            2. Submit the form.

            | ID | Major item | Middle item | Minor item | Steps | Expected result |
            |---|---|---|---|---|---|
            | TC-1 | - | - | Login | - | Dashboard is displayed. |
            """;

        var result = TestSpecificationParser.Parse(source, "login.md");

        var title = Assert.Single(result.Titles);
        Assert.Equal("Verify **login** behavior.", title.OverviewMarkdown);
        Assert.Equal("- User exists.\n- User is active.", title.PreconditionsMarkdown);
        Assert.Equal("1. Open the page.\n2. Submit the form.", title.CommonStepsMarkdown);
    }

    [Fact]
    public void Parse_displays_titles_but_disables_registration_when_version_is_missing()
    {
        var source = ValidSpecification("Login tests", "TC-1")
            .Replace("Testman-Format-Version: 1\n\n", string.Empty);

        var result = TestSpecificationParser.Parse(source, "login.md");

        Assert.Null(result.FormatVersion);
        Assert.Single(result.Titles);
        Assert.All(result.Titles.SelectMany(title => title.TestCases), testCase => Assert.False(testCase.CanRegisterResult));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Reason.Contains("missing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_displays_titles_but_disables_registration_when_version_is_not_numeric()
    {
        var source = ValidSpecification("Login tests", "TC-1")
            .Replace("Testman-Format-Version: 1", "Testman-Format-Version: current");

        var result = TestSpecificationParser.Parse(source, "login.md");

        Assert.Null(result.FormatVersion);
        Assert.Single(result.Titles);
        Assert.All(result.Titles.SelectMany(title => title.TestCases), testCase => Assert.False(testCase.CanRegisterResult));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Reason.Contains("positive integer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_applies_current_rules_to_an_unknown_positive_version()
    {
        var source = ValidSpecification("Login tests", "TC-1")
            .Replace("Testman-Format-Version: 1", "Testman-Format-Version: 42");

        var result = TestSpecificationParser.Parse(source, "login.md");

        Assert.Equal(42, result.FormatVersion);
        Assert.True(Assert.Single(Assert.Single(result.Titles).TestCases).CanRegisterResult);
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

    [Fact]
    public void Parse_omits_a_title_when_table_columns_do_not_match_the_approved_names()
    {
        var source = ValidSpecification("Login tests", "TC-1")
            .Replace("| ID | Major item | Middle item | Minor item | Steps | Expected result |",
                "| ID | Major item | Middle item | Minor item | Steps | Expected Result |");

        var result = TestSpecificationParser.Parse(source, "login.md");

        Assert.Empty(result.Titles);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Reason.Contains("columns", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_omits_a_title_when_table_has_an_extra_column()
    {
        var source = ValidSpecification("Login tests", "TC-1")
            .Replace("| ID | Major item | Middle item | Minor item | Steps | Expected result |",
                "| ID | Major item | Middle item | Minor item | Steps | Expected result | Notes |")
            .Replace("|---|---|---|---|---|---|", "|---|---|---|---|---|---|---|")
            .Replace("| TC-1 | - | - | Login | - | Dashboard is displayed. |",
                "| TC-1 | - | - | Login | - | Dashboard is displayed. | Note |");

        var result = TestSpecificationParser.Parse(source, "login.md");

        Assert.Empty(result.Titles);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Reason.Contains("columns", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_omits_a_title_when_table_has_no_test_case_rows()
    {
        var source = ValidSpecification("Login tests", "TC-1")
            .Replace("| TC-1 | - | - | Login | - | Dashboard is displayed. |", string.Empty);

        var result = TestSpecificationParser.Parse(source, "login.md");

        Assert.Empty(result.Titles);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Reason.Contains("one or more", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_allows_an_empty_steps_cell()
    {
        var source = ValidSpecification("Login tests", "TC-1")
            .Replace("| TC-1 | - | - | Login | - | Dashboard is displayed. |",
                "| TC-1 | - | - | Login |  | Dashboard is displayed. |");

        var result = TestSpecificationParser.Parse(source, "login.md");

        var testCase = Assert.Single(Assert.Single(result.Titles).TestCases);
        Assert.Equal(string.Empty, testCase.Steps);
        Assert.True(testCase.CanRegisterResult);
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData("|  | - | - | Login | - | Dashboard is displayed. |", "ID")]
    [InlineData("| TC-1 |  | - | Login | - | Dashboard is displayed. |", "Major item")]
    [InlineData("| TC-1 | - |  | Login | - | Dashboard is displayed. |", "Middle item")]
    [InlineData("| TC-1 | - | - |  | - | Dashboard is displayed. |", "Minor item")]
    [InlineData("| TC-1 | - | - | Login | - |  |", "Expected result")]
    public void Parse_disables_a_row_when_a_required_cell_is_empty(string replacementRow, string expectedColumn)
    {
        var source = ValidSpecification("Login tests", "TC-1")
            .Replace("| TC-1 | - | - | Login | - | Dashboard is displayed. |", replacementRow);

        var result = TestSpecificationParser.Parse(source, "login.md");

        var testCase = Assert.Single(Assert.Single(result.Titles).TestCases);
        Assert.False(testCase.CanRegisterResult);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Reason.Contains(expectedColumn, StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_disables_a_row_when_test_id_is_invalid()
    {
        var source = ValidSpecification("Login tests", "TC-01");

        var result = TestSpecificationParser.Parse(source, "login.md");

        var testCase = Assert.Single(Assert.Single(result.Titles).TestCases);
        Assert.Equal("TC-01", testCase.Id);
        Assert.False(testCase.CanRegisterResult);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Reason.Contains("Invalid Test ID", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_disables_each_row_with_a_duplicate_test_id()
    {
        var source = ValidSpecification("Login tests", "TC-1")
            .Replace("| TC-1 | - | - | Login | - | Dashboard is displayed. |",
                "| TC-1 | - | - | Login | - | Dashboard is displayed. |\n"
                + "| TC-1 | - | - | Logout | - | Login page is displayed. |");

        var result = TestSpecificationParser.Parse(source, "login.md");

        var testCases = Assert.Single(result.Titles).TestCases;
        Assert.Equal(2, testCases.Count);
        Assert.All(testCases, testCase => Assert.False(testCase.CanRegisterResult));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Reason.Contains("Duplicated ID detected: TC-1", StringComparison.Ordinal));
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
