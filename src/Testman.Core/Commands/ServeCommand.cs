using System.Globalization;

namespace Testman.Core.Commands;

public sealed record ServeCommand(string SpecificationPath, string DatabasePath, int Port)
{
    public const string DefaultDatabasePath = "./.testman/testman.db";
    public const int DefaultPort = 5000;

    private const string Usage = "Usage: testman serve --specs <path> [--db <path>] [--port <1-65535>]";

    public static bool TryParse(
        IReadOnlyList<string> arguments,
        out ServeCommand command,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Count < 3
            || !arguments[0].Equals("serve", StringComparison.Ordinal)
            || (arguments.Count - 1) % 2 != 0)
        {
            return Failure(out command, out error);
        }

        string? specificationPath = null;
        var databasePath = DefaultDatabasePath;
        var port = DefaultPort;
        var seenOptions = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 1; index < arguments.Count; index += 2)
        {
            var option = arguments[index];
            var value = arguments[index + 1];
            if (!seenOptions.Add(option) || string.IsNullOrWhiteSpace(value))
            {
                return Failure(out command, out error);
            }

            switch (option)
            {
                case "--specs":
                    specificationPath = value;
                    break;
                case "--db":
                    databasePath = value;
                    break;
                case "--port" when int.TryParse(
                    value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var parsedPort)
                    && parsedPort is >= 1 and <= 65535:
                    port = parsedPort;
                    break;
                default:
                    return Failure(out command, out error);
            }
        }

        if (specificationPath is null)
        {
            return Failure(out command, out error);
        }

        command = new ServeCommand(specificationPath, databasePath, port);
        error = null;
        return true;
    }

    private static bool Failure(out ServeCommand command, out string? error)
    {
        command = null!;
        error = Usage;
        return false;
    }
}
