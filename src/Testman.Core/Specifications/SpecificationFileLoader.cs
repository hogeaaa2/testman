using System.Text;

namespace Testman.Core.Specifications;

public static class SpecificationFileLoader
{
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    public static SpecificationFileLoadResult Load(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        try
        {
            using var stream = File.OpenRead(sourcePath);
            using var reader = new StreamReader(
                stream,
                StrictUtf8,
                detectEncodingFromByteOrderMarks: false);
            var source = reader.ReadToEnd();
            return new SpecificationFileLoadResult(
                sourcePath,
                TestSpecificationParser.Parse(source, sourcePath));
        }
        catch (FileNotFoundException)
        {
            return Failure(sourcePath, "Specification file was not found.");
        }
        catch (DirectoryNotFoundException)
        {
            return Failure(sourcePath, "Specification file was not found.");
        }
        catch (DecoderFallbackException)
        {
            return Failure(sourcePath, "Specification file is not valid UTF-8.");
        }
        catch (IOException exception)
        {
            return Failure(sourcePath, $"Specification file could not be read: {exception.Message}");
        }
        catch (UnauthorizedAccessException)
        {
            return Failure(sourcePath, "Specification file could not be read because access was denied.");
        }
    }

    private static SpecificationFileLoadResult Failure(string sourcePath, string reason) =>
        new(
            sourcePath,
            new TestSpecificationParseResult(
                null,
                [],
                [new SpecificationDiagnostic(sourcePath, null, reason)]));
}

public sealed record SpecificationFileLoadResult(
    string SourcePath,
    TestSpecificationParseResult Specification);
