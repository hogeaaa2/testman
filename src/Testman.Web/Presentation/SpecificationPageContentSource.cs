using Testman.Core.Specifications;

namespace Testman.Web.Presentation;

public sealed class SpecificationPageContentSource(string specificationPath, string workingDirectory)
{
    public SpecificationPageContent Load() =>
        SpecificationPageContent.Create(
            SpecificationCatalog.Load(specificationPath, workingDirectory));
}
