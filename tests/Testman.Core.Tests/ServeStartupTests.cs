using System.Text;
using Testman.Core.Commands;

namespace Testman.Core.Tests;

public sealed class ServeStartupTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"testman-startup-{Guid.NewGuid():N}");

    public ServeStartupTests() => Directory.CreateDirectory(directory);

    [Fact]
    public void Prepare_loads_the_selected_specifications()
    {
        WriteSpecification("selected.md", "Selected");

        var startup = ServeStartup.Prepare(["serve", "--specs", "selected.md"], directory);

        Assert.True(startup.CanStart);
        Assert.NotNull(startup.Catalog);
        Assert.Equal("Selected", Assert.Single(Assert.Single(startup.Catalog.Files).Specification.Titles).Name);
        Assert.Null(startup.Error);
    }

    [Fact]
    public void Prepare_rejects_invalid_arguments_before_loading_specifications()
    {
        var startup = ServeStartup.Prepare(["serve"], directory);

        Assert.False(startup.CanStart);
        Assert.Null(startup.Catalog);
        Assert.Contains("testman serve --specs <path>", startup.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void Prepare_rejects_a_path_that_cannot_start_the_server()
    {
        var startup = ServeStartup.Prepare(["serve", "--specs", "missing"], directory);

        Assert.False(startup.CanStart);
        Assert.Null(startup.Catalog);
        Assert.Contains("not found", startup.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Prepare_keeps_file_diagnostics_when_the_server_can_start()
    {
        File.WriteAllText(Path.Combine(directory, "invalid.md"), "# Invalid", new UTF8Encoding(false));

        var startup = ServeStartup.Prepare(["serve", "--specs", directory], Path.GetTempPath());

        Assert.True(startup.CanStart);
        Assert.NotNull(startup.Catalog);
        Assert.NotEmpty(startup.Catalog.Diagnostics);
        Assert.Null(startup.Error);
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);

    private void WriteSpecification(string relativePath, string title)
    {
        File.WriteAllText(Path.Combine(directory, relativePath), $$"""
            Testman-Format-Version: 1

            # {{title}}

            ## Overview

            Overview

            ## Preconditions

            None

            ## Common steps

            None

            | ID | Major item | Middle item | Minor item | Steps | Expected result |
            |---|---|---|---|---|---|
            | TC-1 | Major | Middle | Minor | - | Success |
            """, new UTF8Encoding(false));
    }
}
