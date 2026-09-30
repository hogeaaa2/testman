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

            titles.Add(new TestSpecificationTitle(titleName, heading.Line + 1));
        }

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

public sealed record TestSpecificationTitle(string Name, int LineNumber);

public sealed record SpecificationDiagnostic(string SourcePath, int? LineNumber, string Reason);

public sealed record TestSpecificationParseResult(
    IReadOnlyList<TestSpecificationTitle> Titles,
    IReadOnlyList<SpecificationDiagnostic> Diagnostics);
