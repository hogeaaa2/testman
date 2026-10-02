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
        return SpecificationPageContent.Create(
            catalog,
            ReadHistory,
            showFileSummaries: Directory.Exists(fullSpecificationPath));
    }

    private IReadOnlyList<TestResultRecord> ReadHistory(string sourcePath, string testCaseId)
    {
        try
        {
            var identity = GitSpecificationIdentity.Resolve(sourcePath);
            return resultHistory.ReadHistory(identity, testCaseId);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return [];
        }
    }
}
