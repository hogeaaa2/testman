namespace Testman.Core.Specifications;

public static class SpecificationCatalog
{
    public static SpecificationCatalogResult Load(string inputPath, string workingDirectory)
    {
        var resolution = SpecificationPathResolver.Resolve(inputPath, workingDirectory);
        var files = resolution.Paths
            .Select(SpecificationFileLoader.Load)
            .ToList();
        var diagnostics = resolution.Diagnostics
            .Concat(files.SelectMany(file => file.Specification.Diagnostics))
            .ToList();

        return new SpecificationCatalogResult(files, diagnostics);
    }
}

public sealed record SpecificationCatalogResult(
    IReadOnlyList<SpecificationFileLoadResult> Files,
    IReadOnlyList<SpecificationDiagnostic> Diagnostics)
{
    public bool CanStart => Files.Count > 0;
}
