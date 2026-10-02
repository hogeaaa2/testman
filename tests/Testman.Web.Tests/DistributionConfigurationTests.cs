using System.Xml.Linq;

namespace Testman.Web.Tests;

public sealed class DistributionConfigurationTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly XDocument Project = XDocument.Load(
        Path.Combine(RepositoryRoot, "src", "Testman.Web", "Testman.Web.csproj"));

    [Fact]
    public void WebProject_ConfiguresWindowsX64SelfContainedDistribution()
    {
        Assert.Equal("win-x64", Property("RuntimeIdentifier"));
        Assert.Equal("true", Property("SelfContained"));
        Assert.Equal("en;ja;zh-CN", Property("SatelliteResourceLanguages"));
        Assert.Equal("testman", Property("AssemblyName"));
    }

    [Fact]
    public void WebProject_CreatesZipArchiveAfterPublishWhenPackagingIsRequested()
    {
        var target = Project.Descendants("Target")
            .Single(element => (string?)element.Attribute("Name") == "CreateDistributionArchive");

        Assert.Equal("Publish", (string?)target.Attribute("AfterTargets"));
        Assert.Contains("'$(CreateDistributionArchive)' == 'true'", (string?)target.Attribute("Condition"));

        var zip = target.Element("ZipDirectory");
        Assert.NotNull(zip);
        Assert.Equal("$(PublishDir)", (string?)zip.Attribute("SourceDirectory"));
        Assert.Equal("$(DistributionArchivePath)", (string?)zip.Attribute("DestinationFile"));
    }

    private static string Property(string name) =>
        Project.Descendants(name).Single().Value;

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Testman.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Repository root could not be found.");
    }
}
