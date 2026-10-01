using Testman.Core.Rendering;
using Testman.Core.Specifications;

namespace Testman.Web.Presentation;

public sealed record SpecificationPageContent(
    IReadOnlyList<SpecificationDiagnosticContent> Diagnostics,
    IReadOnlyList<SpecificationFileContent> Files)
{
    public static SpecificationPageContent Create(SpecificationCatalogResult catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var diagnostics = catalog.Diagnostics
            .Select(diagnostic => new SpecificationDiagnosticContent(
                diagnostic.SourcePath,
                diagnostic.LineNumber,
                diagnostic.Reason))
            .ToList();

        var files = catalog.Files
            .Where(file => file.Specification.Titles.Count > 0)
            .Select(file => new SpecificationFileContent(
                file.SourcePath,
                file.Specification.FormatVersion,
                file.Specification.Titles.Select(CreateTitle).ToList()))
            .ToList();

        return new SpecificationPageContent(diagnostics, files);
    }

    private static SpecificationTitleContent CreateTitle(TestSpecificationTitle title) =>
        new(
            title.Name,
            SafeMarkdownRenderer.Render(title.OverviewMarkdown),
            title.TestCases.Select(testCase => new SpecificationPatternContent(
                testCase.MajorItem,
                testCase.MiddleItem,
                testCase.MinorItem)).ToList());
}

public sealed record SpecificationDiagnosticContent(
    string SourcePath,
    int? LineNumber,
    string Reason);

public sealed record SpecificationFileContent(
    string SourcePath,
    int? FormatVersion,
    IReadOnlyList<SpecificationTitleContent> Titles);

public sealed record SpecificationTitleContent(
    string Name,
    string OverviewHtml,
    IReadOnlyList<SpecificationPatternContent> Patterns);

public sealed record SpecificationPatternContent(
    string MajorItem,
    string MiddleItem,
    string MinorItem);
