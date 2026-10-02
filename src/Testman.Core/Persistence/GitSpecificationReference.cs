using System.Diagnostics;

namespace Testman.Core.Persistence;

public sealed class GitSpecificationReference
{
    private GitSpecificationReference(
        string repositoryRoot,
        string sourceFile,
        string specificationRevision)
    {
        RepositoryRoot = repositoryRoot;
        SourceFile = sourceFile;
        SpecificationRevision = specificationRevision;
    }

    public string RepositoryRoot { get; }

    public string SourceFile { get; }

    public string SpecificationRevision { get; }

    public static GitSpecificationReference Resolve(string specificationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(specificationPath);

        var fullPath = Path.GetFullPath(specificationPath);
        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException("The specification file does not exist.");
        }

        var containingDirectory = Path.GetDirectoryName(fullPath)!;
        var rootResult = RunGit(containingDirectory, "rev-parse", "--show-toplevel");
        if (rootResult.ExitCode != 0)
        {
            throw new InvalidOperationException("The specification file is not in a Git repository.");
        }

        var repositoryRoot = Path.GetFullPath(rootResult.Output.Trim());
        var relativePath = Path.GetRelativePath(repositoryRoot, fullPath);
        if (Path.IsPathRooted(relativePath)
            || relativePath.Equals("..", StringComparison.Ordinal)
            || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The specification file is outside the Git repository.");
        }

        var sourceFile = relativePath.Replace('\\', '/');
        RequireSuccess(
            RunGit(repositoryRoot, "ls-files", "--error-unmatch", "--", sourceFile),
            "The specification file is not tracked by Git.");
        RequireSuccess(
            RunGit(repositoryRoot, "diff", "--quiet", "HEAD", "--", sourceFile),
            "The specification file has uncommitted working-tree changes.");
        RequireSuccess(
            RunGit(repositoryRoot, "diff", "--cached", "--quiet", "HEAD", "--", sourceFile),
            "The specification file has uncommitted index changes.");

        var revisionResult = RunGit(repositoryRoot, "rev-parse", "HEAD");
        RequireSuccess(revisionResult, "The Git HEAD revision could not be resolved.");

        return new GitSpecificationReference(
            repositoryRoot,
            sourceFile,
            revisionResult.Output.Trim());
    }

    private static void RequireSuccess(GitResult result, string message)
    {
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static GitResult RunGit(string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Git could not be started.");
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return new GitResult(process.ExitCode, output, error);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception
            or InvalidOperationException)
        {
            throw new InvalidOperationException("Git could not be started.", exception);
        }
    }

    private sealed record GitResult(int ExitCode, string Output, string Error);
}
