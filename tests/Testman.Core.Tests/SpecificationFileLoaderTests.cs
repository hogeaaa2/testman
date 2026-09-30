using System.Text;
using Testman.Core.Specifications;

namespace Testman.Core.Tests;

public sealed class SpecificationFileLoaderTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"testman-{Guid.NewGuid():N}");

    public SpecificationFileLoaderTests()
    {
        Directory.CreateDirectory(directory);
    }

    [Fact]
    public void Load_reads_and_parses_an_explicit_utf8_file()
    {
        var path = Path.Combine(directory, "login.md");
        File.WriteAllText(path, ValidSpecification, new UTF8Encoding(false));

        var result = SpecificationFileLoader.Load(path);

        Assert.Equal(path, result.SourcePath);
        Assert.Equal(1, result.Specification.FormatVersion);
        Assert.Equal("ログイン", Assert.Single(result.Specification.Titles).Name);
        Assert.Empty(result.Specification.Diagnostics);
    }

    [Fact]
    public void Load_reports_invalid_utf8_without_parsing_content()
    {
        var path = Path.Combine(directory, "invalid.md");
        File.WriteAllBytes(path, [0xff, 0xfe, 0xfd]);

        var result = SpecificationFileLoader.Load(path);

        Assert.Empty(result.Specification.Titles);
        var diagnostic = Assert.Single(result.Specification.Diagnostics);
        Assert.Equal(path, diagnostic.SourcePath);
        Assert.Contains("UTF-8", diagnostic.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_reports_a_missing_file()
    {
        var path = Path.Combine(directory, "missing.md");

        var result = SpecificationFileLoader.Load(path);

        Assert.Empty(result.Specification.Titles);
        var diagnostic = Assert.Single(result.Specification.Diagnostics);
        Assert.Equal(path, diagnostic.SourcePath);
        Assert.Contains("not found", diagnostic.Reason, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        Directory.Delete(directory, recursive: true);
    }

    private const string ValidSpecification = """
        Testman-Format-Version: 1

        # ログイン

        ## Overview

        概要

        ## Preconditions

        なし

        ## Common steps

        なし

        | ID | Major item | Middle item | Minor item | Steps | Expected result |
        |---|---|---|---|---|---|
        | TC-1 | - | - | ログイン | - | 成功する。 |
        """;
}
