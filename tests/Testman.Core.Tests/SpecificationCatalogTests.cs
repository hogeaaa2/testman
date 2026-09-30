using System.Text;
using Testman.Core.Specifications;

namespace Testman.Core.Tests;

public sealed class SpecificationCatalogTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"testman-catalog-{Guid.NewGuid():N}");

    public SpecificationCatalogTests() => Directory.CreateDirectory(directory);

    [Fact]
    public void Load_combines_specifications_from_the_selected_path()
    {
        WriteSpecification("login.md", "Login", "TC-1");
        WriteSpecification(Path.Combine("account", "profile.md"), "Profile", "TC-1");

        var catalog = SpecificationCatalog.Load(directory, Path.GetTempPath());

        Assert.Equal(2, catalog.Files.Count);
        Assert.Equal(["Login", "Profile"], catalog.Files.SelectMany(file => file.Specification.Titles).Select(title => title.Name).Order());
        Assert.Empty(catalog.Diagnostics);
    }

    [Fact]
    public void Load_keeps_valid_files_when_another_file_has_diagnostics()
    {
        WriteSpecification("valid.md", "Valid", "TC-1");
        File.WriteAllText(Path.Combine(directory, "invalid.md"), "# Missing version and sections", new UTF8Encoding(false));

        var catalog = SpecificationCatalog.Load(directory, Path.GetTempPath());

        Assert.Equal(2, catalog.Files.Count);
        Assert.Contains(catalog.Files, file => file.Specification.Titles.Any(title => title.Name == "Valid"));
        Assert.Contains(catalog.Diagnostics, diagnostic => diagnostic.SourcePath.EndsWith("invalid.md", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_returns_path_resolution_diagnostics()
    {
        var catalog = SpecificationCatalog.Load("missing", directory);

        Assert.Empty(catalog.Files);
        Assert.Contains(catalog.Diagnostics, diagnostic => diagnostic.Reason.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);

    private void WriteSpecification(string relativePath, string title, string id)
    {
        var path = Path.Combine(directory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, $$"""
            Testman-Format-Version: 1

            # {{title}}

            ## Overview

            Overview

            ## Preconditions

            なし

            ## Common steps

            なし

            | ID | Major item | Middle item | Minor item | Steps | Expected result |
            |---|---|---|---|---|---|
            | {{id}} | - | - | Case | - | Success. |
            """, new UTF8Encoding(false));
    }
}
