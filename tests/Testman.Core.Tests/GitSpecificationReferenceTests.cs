using System.Diagnostics;
using Testman.Core.Persistence;

namespace Testman.Core.Tests;

public sealed class GitSpecificationReferenceTests : IDisposable
{
    private readonly string repository = Path.Combine(Path.GetTempPath(), $"testman-git-{Guid.NewGuid():N}");
    private readonly string specificationPath;

    public GitSpecificationReferenceTests()
    {
        Directory.CreateDirectory(Path.Combine(repository, "specs"));
        specificationPath = Path.Combine(repository, "specs", "example.md");
        File.WriteAllText(specificationPath, "# Example");
        Git("init");
        Git("config", "user.name", "Test User");
        Git("config", "user.email", "test@example.invalid");
        Git("add", "specs/example.md");
        Git("commit", "-m", "Add specification");
    }

    [Fact]
    public void Resolve_returns_repository_relative_identity_and_last_file_revision()
    {
        var specificationRevision = GitOutput("log", "-1", "--format=%H", "--", "specs/example.md");
        File.WriteAllText(Path.Combine(repository, "other.txt"), "other");
        Git("add", "other.txt");
        Git("commit", "-m", "Change another file");

        var reference = GitSpecificationReference.Resolve(specificationPath);

        Assert.Equal(Path.GetFullPath(repository), reference.RepositoryRoot);
        Assert.Equal("specs/example.md", reference.SourceFile);
        Assert.Equal(specificationRevision, reference.SpecificationRevision);
        Assert.NotEqual(GitOutput("rev-parse", "HEAD"), reference.SpecificationRevision);
    }

    [Fact]
    public void Resolve_rejects_an_untracked_file()
    {
        var untracked = Path.Combine(repository, "specs", "untracked.md");
        File.WriteAllText(untracked, "# Untracked");

        Assert.Throws<InvalidOperationException>(() => GitSpecificationReference.Resolve(untracked));
    }

    [Fact]
    public void Identity_resolves_an_untracked_file_for_read_only_use()
    {
        var untracked = Path.Combine(repository, "specs", "untracked.md");
        File.WriteAllText(untracked, "# Untracked");

        var identity = GitSpecificationIdentity.Resolve(untracked);

        Assert.Equal(Path.GetFullPath(repository), identity.RepositoryRoot);
        Assert.Equal("specs/untracked.md", identity.SourceFile);
    }

    [Fact]
    public void Resolve_rejects_unstaged_changes_to_the_specification()
    {
        File.AppendAllText(specificationPath, "\nchanged");

        Assert.Throws<InvalidOperationException>(() => GitSpecificationReference.Resolve(specificationPath));
    }

    [Fact]
    public void Resolve_rejects_staged_changes_to_the_specification()
    {
        File.AppendAllText(specificationPath, "\nchanged");
        Git("add", "specs/example.md");

        Assert.Throws<InvalidOperationException>(() => GitSpecificationReference.Resolve(specificationPath));
    }

    [Fact]
    public void ResolveLastCommittedRevision_returns_the_latest_commit_that_changed_the_file()
    {
        var specificationRevision = GitOutput("log", "-1", "--format=%H", "--", "specs/example.md");
        File.WriteAllText(Path.Combine(repository, "other.txt"), "other");
        Git("add", "other.txt");
        Git("commit", "-m", "Change another file");

        var revision = GitSpecificationReference.ResolveLastCommittedRevision(specificationPath);

        Assert.Equal(specificationRevision, revision);
        Assert.NotEqual(GitOutput("rev-parse", "HEAD"), revision);
    }

    [Fact]
    public void ResolveLastCommittedRevision_returns_the_committed_revision_when_the_file_is_dirty()
    {
        var specificationRevision = GitOutput("log", "-1", "--format=%H", "--", "specs/example.md");
        File.AppendAllText(specificationPath, "\nchanged");

        Assert.Equal(
            specificationRevision,
            GitSpecificationReference.ResolveLastCommittedRevision(specificationPath));
    }

    [Fact]
    public void ResolveLastCommittedRevision_reuses_an_existing_identity()
    {
        var identity = GitSpecificationIdentity.Resolve(specificationPath);
        var specificationRevision = GitOutput("log", "-1", "--format=%H", "--", "specs/example.md");
        File.WriteAllText(Path.Combine(repository, "other.txt"), "other");
        Git("add", "other.txt");
        Git("commit", "-m", "Change another file");

        var revision = GitSpecificationReference.ResolveLastCommittedRevision(identity);

        Assert.Equal(specificationRevision, revision);
        Assert.NotEqual(GitOutput("rev-parse", "HEAD"), revision);
    }

    [Fact]
    public void ResolveLastCommittedRevision_rejects_a_null_identity()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GitSpecificationReference.ResolveLastCommittedRevision((GitSpecificationIdentity)null!));
    }

    [Fact]
    public void ResolveLastCommittedRevision_rejects_an_untracked_file()
    {
        var untracked = Path.Combine(repository, "specs", "untracked.md");
        File.WriteAllText(untracked, "# Untracked");

        Assert.Throws<InvalidOperationException>(() =>
            GitSpecificationReference.ResolveLastCommittedRevision(untracked));
    }

    public void Dispose()
    {
        foreach (var entry in new DirectoryInfo(repository).EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
        {
            entry.Attributes = FileAttributes.Normal;
        }

        Directory.Delete(repository, recursive: true);
    }

    private void Git(params string[] arguments)
    {
        var result = RunGit(arguments);
        Assert.True(result.ExitCode == 0, result.Error);
    }

    private string GitOutput(params string[] arguments)
    {
        var result = RunGit(arguments);
        Assert.True(result.ExitCode == 0, result.Error);
        return result.Output.Trim();
    }

    private GitResult RunGit(IEnumerable<string> arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = repository,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new GitResult(process.ExitCode, output, error);
    }

    private sealed record GitResult(int ExitCode, string Output, string Error);
}
