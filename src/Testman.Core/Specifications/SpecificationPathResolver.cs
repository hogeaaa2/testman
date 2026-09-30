namespace Testman.Core.Specifications;

public static class SpecificationPathResolver
{
    public static SpecificationPathResolution Resolve(string inputPath, string workingDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        var resolvedPath = Path.GetFullPath(inputPath, workingDirectory);
        var diagnostics = new List<SpecificationDiagnostic>();

        if (File.Exists(resolvedPath))
        {
            if (IsReparsePoint(resolvedPath))
            {
                diagnostics.Add(NoMarkdownDiagnostic(inputPath));
                return new SpecificationPathResolution([], diagnostics);
            }

            if (!HasMarkdownExtension(resolvedPath))
            {
                diagnostics.Add(new SpecificationDiagnostic(inputPath, null, "Specified file is not a Markdown specification."));
                return new SpecificationPathResolution([], diagnostics);
            }

            return new SpecificationPathResolution([resolvedPath], diagnostics);
        }

        if (!Directory.Exists(resolvedPath))
        {
            diagnostics.Add(new SpecificationDiagnostic(inputPath, null, "Specification path was not found."));
            return new SpecificationPathResolution([], diagnostics);
        }

        if (IsReparsePoint(resolvedPath))
        {
            diagnostics.Add(NoMarkdownDiagnostic(inputPath));
            return new SpecificationPathResolution([], diagnostics);
        }

        var paths = new List<string>();
        var pending = new Stack<DirectoryInfo>();
        pending.Push(new DirectoryInfo(resolvedPath));

        while (pending.TryPop(out var current))
        {
            try
            {
                foreach (var entry in current.EnumerateFileSystemInfos())
                {
                    if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        continue;
                    }

                    if (entry is DirectoryInfo childDirectory)
                    {
                        pending.Push(childDirectory);
                    }
                    else if (entry is FileInfo file && HasMarkdownExtension(file.Name))
                    {
                        paths.Add(file.FullName);
                    }
                }
            }
            catch (IOException exception)
            {
                diagnostics.Add(new SpecificationDiagnostic(current.FullName, null, $"Specification directory could not be read: {exception.Message}"));
            }
            catch (UnauthorizedAccessException)
            {
                diagnostics.Add(new SpecificationDiagnostic(current.FullName, null, "Specification directory could not be read because access was denied."));
            }
        }

        if (paths.Count == 0)
        {
            diagnostics.Add(NoMarkdownDiagnostic(inputPath));
        }

        return new SpecificationPathResolution(paths, diagnostics);
    }

    private static bool HasMarkdownExtension(string path) =>
        Path.GetExtension(path).Equals(".md", StringComparison.Ordinal);

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static SpecificationDiagnostic NoMarkdownDiagnostic(string inputPath) =>
        new(inputPath, null, "Specification path contains no Markdown specification files.");
}

public sealed record SpecificationPathResolution(
    IReadOnlyList<string> Paths,
    IReadOnlyList<SpecificationDiagnostic> Diagnostics);
