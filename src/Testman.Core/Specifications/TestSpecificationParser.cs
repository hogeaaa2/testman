using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Text;

namespace Testman.Core.Specifications;

public static class TestSpecificationParser
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static readonly string[] RequiredSections = ["Overview", "Preconditions", "Common steps"];
    private static readonly string[] RequiredColumns =
        ["ID", "Major item", "Middle item", "Minor item", "Steps", "Expected result"];
    private static readonly int[] RequiredCellIndexes = [0, 1, 2, 3, 5];

    public static TestSpecificationParseResult Parse(string source, string sourcePath)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        var document = Markdown.Parse(source, Pipeline);
        var blocks = document.ToList();
        var titles = new List<TestSpecificationTitle>();
        var diagnostics = new List<SpecificationDiagnostic>();

        var titleIndexes = blocks
            .Select((block, index) => (block, index))
            .Where(item => item.block is HeadingBlock { Level: 1 })
            .Select(item => item.index)
            .ToList();

        for (var titlePosition = 0; titlePosition < titleIndexes.Count; titlePosition++)
        {
            var start = titleIndexes[titlePosition];
            var end = titlePosition + 1 < titleIndexes.Count ? titleIndexes[titlePosition + 1] : blocks.Count;
            var heading = (HeadingBlock)blocks[start];
            var titleName = ReadHeadingText(source, heading);
            var titleBlocks = blocks.GetRange(start + 1, end - start - 1);

            var sectionNames = titleBlocks
                .OfType<HeadingBlock>()
                .Where(section => section.Level == 2)
                .Select(section => ReadHeadingText(source, section))
                .ToList();

            var missingSection = RequiredSections.FirstOrDefault(required => !sectionNames.Contains(required, StringComparer.Ordinal));
            if (missingSection is not null)
            {
                diagnostics.Add(new SpecificationDiagnostic(sourcePath, heading.Line + 1, $"Required section '{missingSection}' is missing."));
                continue;
            }

            if (!sectionNames.SequenceEqual(RequiredSections, StringComparer.Ordinal))
            {
                diagnostics.Add(new SpecificationDiagnostic(sourcePath, heading.Line + 1, "Required sections are not in the approved order."));
                continue;
            }

            var tables = titleBlocks
                .Select((block, index) => (block, index))
                .Where(item => item.block is Table)
                .ToList();

            var sectionIndexes = RequiredSections
                .Select(required => titleBlocks.FindIndex(block =>
                    block is HeadingBlock { Level: 2 } section
                    && ReadHeadingText(source, section) == required))
                .ToArray();

            var emptySection = RequiredSections
                .Select((name, index) => new
                {
                    Name = name,
                    Start = sectionIndexes[index] + 1,
                    End = index + 1 < sectionIndexes.Length
                        ? sectionIndexes[index + 1]
                        : tables.Count > 0 ? tables[0].index : titleBlocks.Count,
                })
                .FirstOrDefault(section => section.Start >= section.End);

            if (emptySection is not null)
            {
                diagnostics.Add(new SpecificationDiagnostic(sourcePath, heading.Line + 1, $"Required section '{emptySection.Name}' must not be empty."));
                continue;
            }

            if (tables.Count != 1 || tables[0].index <= sectionIndexes[^1])
            {
                diagnostics.Add(new SpecificationDiagnostic(sourcePath, heading.Line + 1, "The title must contain exactly one test case table after Common steps."));
                continue;
            }

            var table = (Table)tables[0].block;
            var rows = table.OfType<TableRow>().ToList();
            var headerRow = rows.SingleOrDefault(row => row.IsHeader);
            var columns = headerRow?
                .OfType<TableCell>()
                .Select(ReadCellText)
                .ToArray();

            if (columns is null || !columns.SequenceEqual(RequiredColumns, StringComparer.Ordinal))
            {
                diagnostics.Add(new SpecificationDiagnostic(sourcePath, table.Line + 1, "Test case table columns do not match the approved names and order."));
                continue;
            }

            if (!rows.Any(row => !row.IsHeader))
            {
                diagnostics.Add(new SpecificationDiagnostic(sourcePath, table.Line + 1, "Test case table must contain one or more test case rows."));
                continue;
            }

            var testCases = new List<TestSpecificationCase>();
            foreach (var row in rows.Where(row => !row.IsHeader))
            {
                var cells = row
                    .OfType<TableCell>()
                    .Select(cell => ReadCellSource(source, cell))
                    .ToArray();

                var canRegisterResult = true;
                if (cells.Length != RequiredColumns.Length)
                {
                    diagnostics.Add(new SpecificationDiagnostic(sourcePath, row.Line + 1, "Test case row does not contain the approved number of cells."));
                    canRegisterResult = false;
                    Array.Resize(ref cells, RequiredColumns.Length);
                    for (var index = 0; index < cells.Length; index++)
                    {
                        cells[index] ??= string.Empty;
                    }
                }

                foreach (var requiredIndex in RequiredCellIndexes.Where(index => string.IsNullOrEmpty(cells[index])))
                {
                    diagnostics.Add(new SpecificationDiagnostic(sourcePath, row.Line + 1, $"Required cell '{RequiredColumns[requiredIndex]}' must not be empty."));
                    canRegisterResult = false;
                }

                if (!string.IsNullOrEmpty(cells[0]) && !TestId.TryParse(cells[0], out _))
                {
                    diagnostics.Add(new SpecificationDiagnostic(sourcePath, row.Line + 1, $"Invalid Test ID: {cells[0]}"));
                    canRegisterResult = false;
                }

                testCases.Add(new TestSpecificationCase(
                    cells[0],
                    cells[1],
                    cells[2],
                    cells[3],
                    cells[4],
                    cells[5],
                    row.Line + 1,
                    canRegisterResult));
            }

            titles.Add(new TestSpecificationTitle(titleName, heading.Line + 1, testCases));
        }

        DisableDuplicateIds(titles, diagnostics, sourcePath);

        return new TestSpecificationParseResult(titles, diagnostics);
    }

    private static string ReadHeadingText(string source, HeadingBlock heading)
    {
        var lineStart = heading.Span.Start;
        var lineLength = heading.Span.End - heading.Span.Start + 1;
        var line = source.AsSpan(lineStart, lineLength).Trim();
        return line.TrimStart('#').Trim().TrimEnd('#').Trim().ToString();
    }

    private static string ReadCellText(TableCell cell)
    {
        var builder = new StringBuilder();

        foreach (var paragraph in cell.OfType<ParagraphBlock>())
        {
            AppendInlineText(paragraph.Inline, builder);
        }

        return builder.ToString();
    }

    private static string ReadCellSource(string source, TableCell cell)
    {
        if (cell.Span.Start < 0 || cell.Span.End < cell.Span.Start)
        {
            return ReadCellText(cell);
        }

        return source.AsSpan(cell.Span.Start, cell.Span.End - cell.Span.Start + 1).Trim().ToString();
    }

    private static void DisableDuplicateIds(
        List<TestSpecificationTitle> titles,
        List<SpecificationDiagnostic> diagnostics,
        string sourcePath)
    {
        var duplicateIds = titles
            .SelectMany(title => title.TestCases)
            .Where(testCase => TestId.TryParse(testCase.Id, out _))
            .GroupBy(testCase => testCase.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        if (duplicateIds.Count == 0)
        {
            return;
        }

        for (var titleIndex = 0; titleIndex < titles.Count; titleIndex++)
        {
            var title = titles[titleIndex];
            titles[titleIndex] = title with
            {
                TestCases = title.TestCases
                    .Select(testCase => duplicateIds.Contains(testCase.Id)
                        ? testCase with { CanRegisterResult = false }
                        : testCase)
                    .ToList(),
            };
        }

        diagnostics.Add(new SpecificationDiagnostic(
            sourcePath,
            null,
            $"Duplicated ID detected: {string.Join(", ", duplicateIds.Order(StringComparer.Ordinal))}"));
    }

    private static void AppendInlineText(ContainerInline? container, StringBuilder builder)
    {
        if (container is null)
        {
            return;
        }

        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content);
                    break;
                case CodeInline code:
                    builder.Append(code.Content);
                    break;
                case ContainerInline nested:
                    AppendInlineText(nested, builder);
                    break;
            }
        }
    }
}

public sealed record TestSpecificationTitle(
    string Name,
    int LineNumber,
    IReadOnlyList<TestSpecificationCase> TestCases);

public sealed record TestSpecificationCase(
    string Id,
    string MajorItem,
    string MiddleItem,
    string MinorItem,
    string Steps,
    string ExpectedResult,
    int LineNumber,
    bool CanRegisterResult);

public sealed record SpecificationDiagnostic(string SourcePath, int? LineNumber, string Reason);

public sealed record TestSpecificationParseResult(
    IReadOnlyList<TestSpecificationTitle> Titles,
    IReadOnlyList<SpecificationDiagnostic> Diagnostics);
