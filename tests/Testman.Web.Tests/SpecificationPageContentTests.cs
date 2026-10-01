using Testman.Core.Specifications;
using Testman.Web.Presentation;

namespace Testman.Web.Tests;

public sealed class SpecificationPageContentTests
{
    [Fact]
    public void Create_keeps_valid_titles_when_another_file_has_diagnostics()
    {
        var valid = Parse("valid.md", ValidSpecification("Valid title", "<strong>Overview</strong>"));
        var invalid = Parse("invalid.md", "Testman-Format-Version: 1\n\n# Invalid title");
        var catalog = new SpecificationCatalogResult(
            [valid, invalid],
            valid.Specification.Diagnostics.Concat(invalid.Specification.Diagnostics).ToList());

        var content = SpecificationPageContent.Create(catalog);

        var file = Assert.Single(content.Files);
        Assert.Equal("valid.md", file.SourcePath);
        Assert.Equal(1, file.FormatVersion);
        Assert.Equal("Valid title", Assert.Single(file.Titles).Name);
        var diagnostic = Assert.Single(content.Diagnostics);
        Assert.Equal("invalid.md", diagnostic.SourcePath);
        Assert.Equal(3, diagnostic.LineNumber);
        Assert.Contains("Required section", diagnostic.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_sanitizes_overview_html_and_exposes_pattern_classification_without_test_ids()
    {
        var file = Parse(
            "unsafe.md",
            ValidSpecification("Unsafe", "Overview<script>alert('x')</script>"));
        var catalog = new SpecificationCatalogResult([file], file.Specification.Diagnostics);

        var content = SpecificationPageContent.Create(catalog);

        var title = Assert.Single(Assert.Single(content.Files).Titles);
        Assert.Contains("Overview", title.OverviewHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("script", title.OverviewHtml, StringComparison.OrdinalIgnoreCase);
        var pattern = Assert.Single(title.Patterns);
        Assert.Equal("Major", pattern.MajorItem);
        Assert.Equal("Middle", pattern.MiddleItem);
        Assert.Equal("Minor", pattern.MinorItem);
        Assert.DoesNotContain("TC-1", title.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Create_prepares_sanitized_verification_details()
    {
        var source = ValidSpecification("Verification", "Overview")
            .Replace("None\n\n## Common steps", "Precondition <script>unsafe()</script>\n\n## Common steps", StringComparison.Ordinal)
            .Replace("None\n\n| ID", "Common **step**\n\n| ID", StringComparison.Ordinal)
            .Replace("| TC-1 | Major | Middle | Minor | - | Success |", "| TC-1 | Major | Middle | Minor | Click **save** | Shows <em>success</em> |", StringComparison.Ordinal);
        var file = Parse("verification.md", source);

        var content = SpecificationPageContent.Create(
            new SpecificationCatalogResult([file], file.Specification.Diagnostics));

        var title = Assert.Single(Assert.Single(content.Files).Titles);
        Assert.Contains("Precondition", title.PreconditionsHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("script", title.PreconditionsHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<strong>step</strong>", title.CommonStepsHtml, StringComparison.Ordinal);
        var testCase = Assert.Single(title.VerificationCases);
        Assert.Equal("TC-1", testCase.Id);
        Assert.Contains("<strong>save</strong>", testCase.StepsHtml, StringComparison.Ordinal);
        Assert.Contains("<em>success</em>", testCase.ExpectedResultHtml, StringComparison.Ordinal);
        Assert.True(testCase.CanRegisterResult);
    }

    private static SpecificationFileLoadResult Parse(string path, string source) =>
        new(path, TestSpecificationParser.Parse(source, path));

    private static string ValidSpecification(string title, string overview) => $$"""
        Testman-Format-Version: 1

        # {{title}}

        ## Overview

        {{overview}}

        ## Preconditions

        None

        ## Common steps

        None

        | ID | Major item | Middle item | Minor item | Steps | Expected result |
        |---|---|---|---|---|---|
        | TC-1 | Major | Middle | Minor | - | Success |
        """;
}
