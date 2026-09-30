using Testman.Core.Commands;

namespace Testman.Core.Tests;

public sealed class ServeCommandTests
{
    [Theory]
    [InlineData("specs")]
    [InlineData("C:\\work\\specs")]
    [InlineData("./testspecs/login.md")]
    public void TryParse_accepts_serve_with_a_specification_path(string path)
    {
        var parsed = ServeCommand.TryParse(["serve", "--specs", path], out var command, out var error);

        Assert.True(parsed);
        Assert.Equal(path, command.SpecificationPath);
        Assert.Null(error);
    }

    [Theory]
    [MemberData(nameof(InvalidArguments))]
    public void TryParse_rejects_arguments_outside_the_v01_command(string[] arguments)
    {
        var parsed = ServeCommand.TryParse(arguments, out _, out var error);

        Assert.False(parsed);
        Assert.NotNull(error);
        Assert.Contains("testman serve --specs <path>", error, StringComparison.Ordinal);
    }

    public static TheoryData<string[]> InvalidArguments => new()
    {
        Array.Empty<string>(),
        new[] { "serve" },
        new[] { "serve", "--specs" },
        new[] { "run", "--specs", "specs" },
        new[] { "serve", "--unknown", "specs" },
        new[] { "serve", "--specs", "specs", "extra" },
    };
}
