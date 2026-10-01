using Testman.Core.Specifications;

namespace Testman.Core.Tests;

public sealed class SpecificationPathResolverTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"testman-paths-{Guid.NewGuid():N}");
    private readonly List<string> symbolicLinks = [];

    public SpecificationPathResolverTests() => Directory.CreateDirectory(directory);

    [Fact]
    public void Resolve_returns_only_an_explicit_markdown_file()
    {
        var selected = WriteFile("selected.md");
        WriteFile("other.md");

        var result = SpecificationPathResolver.Resolve(selected, directory);

        Assert.Equal([selected], result.Paths);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Resolve_reads_markdown_files_recursively_and_ignores_other_extensions()
    {
        var rootFile = WriteFile("root.md");
        var nestedFile = WriteFile(Path.Combine("nested", "child.md"));
        WriteFile("notes.txt");

        var result = SpecificationPathResolver.Resolve(directory, Path.GetTempPath());

        Assert.Equal(
            new[] { rootFile, nestedFile }.Order(StringComparer.Ordinal),
            result.Paths.Order(StringComparer.Ordinal));
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Resolve_uses_the_working_directory_for_a_relative_path()
    {
        var selected = WriteFile(Path.Combine("specs", "selected.md"));

        var result = SpecificationPathResolver.Resolve(Path.Combine("specs", "selected.md"), directory);

        Assert.Equal([selected], result.Paths);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Resolve_reports_a_missing_path()
    {
        var result = SpecificationPathResolver.Resolve("missing", directory);

        Assert.Empty(result.Paths);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Reason.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Resolve_reports_a_directory_without_markdown_files()
    {
        WriteFile("notes.txt");

        var result = SpecificationPathResolver.Resolve(directory, Path.GetTempPath());

        Assert.Empty(result.Paths);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Reason.Contains("no Markdown", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Resolve_reports_an_explicit_non_markdown_file()
    {
        var path = WriteFile("notes.txt");

        var result = SpecificationPathResolver.Resolve(path, directory);

        Assert.Empty(result.Paths);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Reason.Contains("Markdown", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Resolve_ignores_symbolic_links_to_markdown_files()
    {
        var regularFile = WriteFile("regular.md");
        var targetFile = WriteFile(Path.Combine("targets", "target.md"));
        var linkPath = Path.Combine(directory, "linked.md");
        File.CreateSymbolicLink(linkPath, targetFile);
        symbolicLinks.Add(linkPath);

        var result = SpecificationPathResolver.Resolve(directory, Path.GetTempPath());

        Assert.Contains(regularFile, result.Paths);
        Assert.Contains(targetFile, result.Paths);
        Assert.DoesNotContain(linkPath, result.Paths);
    }

    [Fact]
    public void Resolve_does_not_recurse_into_symbolic_link_directories()
    {
        var regularFile = WriteFile("regular.md");
        var externalDirectory = Path.Combine(Path.GetTempPath(), $"testman-linked-target-{Guid.NewGuid():N}");
        Directory.CreateDirectory(externalDirectory);
        File.WriteAllText(Path.Combine(externalDirectory, "external.md"), string.Empty);
        var linkPath = Path.Combine(directory, "linked-directory");
        Directory.CreateSymbolicLink(linkPath, externalDirectory);
        symbolicLinks.Add(linkPath);

        try
        {
            var result = SpecificationPathResolver.Resolve(directory, Path.GetTempPath());

            Assert.Equal([regularFile], result.Paths);
        }
        finally
        {
            Directory.Delete(externalDirectory, recursive: true);
        }
    }

    public void Dispose()
    {
        foreach (var symbolicLink in symbolicLinks)
        {
            if (File.Exists(symbolicLink))
            {
                File.Delete(symbolicLink);
            }
            else if (Directory.Exists(symbolicLink))
            {
                Directory.Delete(symbolicLink);
            }
        }

        Directory.Delete(directory, recursive: true);
    }

    private string WriteFile(string relativePath)
    {
        var path = Path.Combine(directory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, string.Empty);
        return Path.GetFullPath(path);
    }
}
