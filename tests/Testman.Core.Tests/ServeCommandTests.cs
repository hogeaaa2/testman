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
        Assert.Equal("./.testman/testman.db", command.DatabasePath);
        Assert.Equal(5000, command.Port);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("--db", "results/testman.db", "--port", "12345")]
    [InlineData("--port", "12345", "--db", "results/testman.db")]
    public void TryParse_accepts_optional_database_and_port_in_either_order(
        string firstOption,
        string firstValue,
        string secondOption,
        string secondValue)
    {
        var parsed = ServeCommand.TryParse(
            ["serve", "--specs", "specs", firstOption, firstValue, secondOption, secondValue],
            out var command,
            out var error);

        Assert.True(parsed);
        Assert.Equal("specs", command.SpecificationPath);
        Assert.Equal("results/testman.db", command.DatabasePath);
        Assert.Equal(12345, command.Port);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("65535")]
    public void TryParse_accepts_the_supported_port_boundaries(string value)
    {
        var parsed = ServeCommand.TryParse(
            ["serve", "--specs", "specs", "--port", value],
            out var command,
            out _);

        Assert.True(parsed);
        Assert.Equal(int.Parse(value), command.Port);
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
        new[] { "serve", "--specs", "specs", "--db" },
        new[] { "serve", "--specs", "specs", "--db", " " },
        new[] { "serve", "--specs", "specs", "--db", "one.db", "--db", "two.db" },
        new[] { "serve", "--specs", "specs", "--port" },
        new[] { "serve", "--specs", "specs", "--port", "0" },
        new[] { "serve", "--specs", "specs", "--port", "65536" },
        new[] { "serve", "--specs", "specs", "--port", "not-a-number" },
        new[] { "serve", "--specs", "specs", "--port", "5000", "--port", "5001" },
        new[] { "serve", "--specs", "one", "--specs", "two" },
    };
}
