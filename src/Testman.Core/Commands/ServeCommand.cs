namespace Testman.Core.Commands;

public sealed record ServeCommand(string SpecificationPath)
{
    private const string Usage = "Usage: testman serve --specs <path>";

    public static bool TryParse(
        IReadOnlyList<string> arguments,
        out ServeCommand command,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Count == 3
            && arguments[0].Equals("serve", StringComparison.Ordinal)
            && arguments[1].Equals("--specs", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(arguments[2]))
        {
            command = new ServeCommand(arguments[2]);
            error = null;
            return true;
        }

        command = null!;
        error = Usage;
        return false;
    }
}
