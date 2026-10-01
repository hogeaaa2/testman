using Testman.Core.Specifications;

namespace Testman.Core.Commands;

public static class ServeStartup
{
    public static ServeStartupResult Prepare(IReadOnlyList<string> arguments, string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        if (!ServeCommand.TryParse(arguments, out var command, out var error))
        {
            return ServeStartupResult.Failure(error!);
        }

        var catalog = SpecificationCatalog.Load(command.SpecificationPath, workingDirectory);
        if (!catalog.CanStart)
        {
            return ServeStartupResult.Failure(string.Join(
                Environment.NewLine,
                catalog.Diagnostics.Select(FormatDiagnostic)));
        }

        return ServeStartupResult.Success(command, catalog);
    }

    private static string FormatDiagnostic(SpecificationDiagnostic diagnostic)
    {
        var location = diagnostic.LineNumber is int lineNumber
            ? $"{diagnostic.SourcePath}:{lineNumber}"
            : diagnostic.SourcePath;
        return $"{location}: {diagnostic.Reason}";
    }
}

public sealed record ServeStartupResult(
    bool CanStart,
    ServeCommand? Command,
    SpecificationCatalogResult? Catalog,
    string? Error)
{
    internal static ServeStartupResult Success(ServeCommand command, SpecificationCatalogResult catalog) =>
        new(true, command, catalog, null);

    internal static ServeStartupResult Failure(string error) =>
        new(false, null, null, error);
}
