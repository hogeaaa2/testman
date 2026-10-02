using System.Diagnostics;

namespace Testman.Core.Persistence;

public sealed class GitSpecificationReference
{
    private GitSpecificationReference(
        GitSpecificationIdentity identity,
        string specificationRevision)
    {
        Identity = identity;
        SpecificationRevision = specificationRevision;
    }

    public GitSpecificationIdentity Identity { get; }

    public string RepositoryRoot => Identity.RepositoryRoot;

    public string SourceFile => Identity.SourceFile;

    public string SpecificationRevision { get; }

    public static GitSpecificationReference Resolve(string specificationPath)
    {
        var identity = GitSpecificationIdentity.Resolve(specificationPath);
        RequireSuccess(
            RunGit(identity.RepositoryRoot, "ls-files", "--error-unmatch", "--", identity.SourceFile),
            "The specification file is not tracked by Git.");
        RequireSuccess(
            RunGit(identity.RepositoryRoot, "diff", "--quiet", "HEAD", "--", identity.SourceFile),
            "The specification file has uncommitted working-tree changes.");
        RequireSuccess(
            RunGit(identity.RepositoryRoot, "diff", "--cached", "--quiet", "HEAD", "--", identity.SourceFile),
            "The specification file has uncommitted index changes.");

        var revisionResult = RunGit(identity.RepositoryRoot, "rev-parse", "HEAD");
        RequireSuccess(revisionResult, "The Git HEAD revision could not be resolved.");

        return new GitSpecificationReference(
            identity,
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

public sealed class GitSpecificationIdentity
{
    private GitSpecificationIdentity(string repositoryRoot, string sourceFile)
    {
        RepositoryRoot = repositoryRoot;
        SourceFile = sourceFile;
    }

    public string RepositoryRoot { get; }

    public string SourceFile { get; }

    public static GitSpecificationIdentity Resolve(string specificationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(specificationPath);

        var fullPath = Path.GetFullPath(specificationPath);
        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException("The specification file does not exist.");
        }

        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = Path.GetDirectoryName(fullPath)!,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("rev-parse");
        startInfo.ArgumentList.Add("--show-toplevel");

        string repositoryRoot;
        try
        {
            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Git could not be started.");
            var output = process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("The specification file is not in a Git repository.");
            }

            repositoryRoot = Path.GetFullPath(output.Trim());
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            throw new InvalidOperationException("Git could not be started.", exception);
        }

        var relativePath = Path.GetRelativePath(repositoryRoot, fullPath);
        if (Path.IsPathRooted(relativePath)
            || relativePath.Equals("..", StringComparison.Ordinal)
            || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The specification file is outside the Git repository.");
        }

        return new GitSpecificationIdentity(repositoryRoot, relativePath.Replace('\\', '/'));
    }
}
