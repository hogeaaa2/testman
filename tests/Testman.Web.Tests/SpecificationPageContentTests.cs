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
