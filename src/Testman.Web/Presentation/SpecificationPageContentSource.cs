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
        return SpecificationPageContent.Create(catalog, ReadHistory);
    }

    private IReadOnlyList<TestResultRecord> ReadHistory(string sourcePath, string testCaseId)
    {
        try
        {
            var identity = GitSpecificationIdentity.Resolve(sourcePath);
            return resultHistory.ReadHistory(identity, testCaseId);
        }
        catch (InvalidOperationException)
        {
            return [];
        }
    }
}
