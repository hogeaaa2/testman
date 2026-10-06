using Testman.Core.Persistence;
using Testman.Core.Specifications;

namespace Testman.Web.Presentation;

public sealed class SpecificationPageContentSource(
    string specificationPath,
    string workingDirectory,
    ResultHistoryStore resultHistory)
{
    public SpecificationPageContent Load()
    {
        var catalog = SpecificationCatalog.Load(specificationPath, workingDirectory);
        var fullSpecificationPath = Path.GetFullPath(specificationPath, workingDirectory);
        var identities = new Dictionary<string, GitSpecificationIdentity?>(
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        return SpecificationPageContent.Create(
            catalog,
            ReadHistory,
            showFileSummaries: Directory.Exists(fullSpecificationPath),
            resolveRevision: ResolveRevision);

        GitSpecificationIdentity? ResolveIdentity(string sourcePath)
        {
            if (identities.TryGetValue(sourcePath, out var identity))
            {
                return identity;
            }

            try
            {
                identity = GitSpecificationIdentity.Resolve(sourcePath);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                identity = null;
            }

            identities.Add(sourcePath, identity);
            return identity;
        }

        IReadOnlyList<TestResultRecord> ReadHistory(string sourcePath, string testCaseId)
        {
            try
            {
                var identity = ResolveIdentity(sourcePath);
                return identity is null ? [] : resultHistory.ReadHistory(identity, testCaseId);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                return [];
            }
        }

        string? ResolveRevision(string sourcePath)
        {
            try
            {
                var identity = ResolveIdentity(sourcePath);
                return identity is null ? null : GitSpecificationReference.ResolveLastCommittedRevision(identity);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                return null;
            }
        }
    }
}
