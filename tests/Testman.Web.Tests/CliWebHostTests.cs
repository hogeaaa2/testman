using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Testman.Core.Persistence;

namespace Testman.Web.Tests;

public sealed class CliWebHostTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"testman-web-{Guid.NewGuid():N}");

    public CliWebHostTests() => Directory.CreateDirectory(directory);

    [Fact]
    public async Task Invalid_arguments_exit_with_an_error_without_starting_the_server()
    {
        using var process = StartProcess(["serve"]);

        try
        {
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal(1, process.ExitCode);
            Assert.Contains("testman serve --specs <path>", process.StandardError.ReadToEnd(), StringComparison.Ordinal);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
    }

    [Fact]
    public async Task Database_initialization_failure_exits_with_an_error_without_starting_the_server()
    {
        WriteFile("valid.md", ValidSpecification("Valid title"));
        var databaseDirectory = Path.Combine(directory, "database-as-directory");
        Directory.CreateDirectory(databaseDirectory);
        using var process = StartProcess(
            ["serve", "--specs", "valid.md", "--db", databaseDirectory, "--port", FindAvailablePort().ToString()]);

        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        var error = process.StandardError.ReadToEnd();

        Assert.Equal(1, process.ExitCode);
        Assert.Contains("Database could not be initialized", error, StringComparison.Ordinal);
        Assert.DoesNotContain("Unhandled exception", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Home_page_displays_valid_titles_and_diagnostics_from_the_selected_path()
    {
        WriteFile("valid.md", ValidSpecification("Valid title"));
        WriteFile("invalid.md", "Testman-Format-Version: 1\n\n# Invalid title");
        var port = FindAvailablePort();
        var ignoredEnvironmentPort = FindAvailablePort();
        var relativeDatabasePath = Path.Combine("data", "results.db");
        using var process = StartProcess(
            ["serve", "--specs", ".", "--db", relativeDatabasePath, "--port", port.ToString()],
            new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["Kestrel__Endpoints__Http__Url"] = $"http://localhost:{ignoredEnvironmentPort}",
            });

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
            var html = await GetWhenReady(client, process);

            var databasePath = Path.Combine(directory, relativeDatabasePath);
            Assert.True(File.Exists(databasePath));
            using (var connection = SqliteConnectionFactory.Open(databasePath, pooling: false))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT name FROM schema_migrations WHERE version = 1";
                Assert.Equal("create_result_history", command.ExecuteScalar());
            }

            Assert.Contains("Valid title", html, StringComparison.Ordinal);
            Assert.Contains("Safe overview", html, StringComparison.Ordinal);
            Assert.DoesNotContain("alert('unsafe')", html, StringComparison.Ordinal);
            Assert.Contains("Major", html, StringComparison.Ordinal);
            Assert.Contains("Middle", html, StringComparison.Ordinal);
            Assert.Contains("Minor", html, StringComparison.Ordinal);
            Assert.Contains("invalid.md", html, StringComparison.Ordinal);
            Assert.Contains("Required section", html, StringComparison.Ordinal);
            Assert.DoesNotContain("TC-1", html, StringComparison.Ordinal);
            Assert.Contains("Test patterns", html, StringComparison.Ordinal);
            Assert.Contains("Verification", html, StringComparison.Ordinal);
            Assert.Contains("/lib/bootstrap/dist/css/bootstrap.min.", html, StringComparison.Ordinal);
            Assert.DoesNotContain("cdn.", html, StringComparison.OrdinalIgnoreCase);

            WriteFile("valid.md", ValidSpecification("Updated title"));
            var updatedHtml = await client.GetStringAsync("/");

            Assert.Contains("Updated title", updatedHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("Valid title", updatedHtml, StringComparison.Ordinal);

            var verificationHtml = await client.GetStringAsync("/?mode=verification");

            Assert.Contains("TC-1", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("Verification precondition", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("Shared step", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("Case step", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("Expected success", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("Previous result", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("Not Tested", verificationHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("Safe overview", verificationHtml, StringComparison.Ordinal);
        }
        finally
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);

    private Process StartProcess(
        IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = directory,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        startInfo.ArgumentList.Add(typeof(Program).Assembly.Location);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environment is not null)
        {
            foreach (var pair in environment)
            {
                startInfo.Environment[pair.Key] = pair.Value;
            }
        }

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the Testman Web process.");
    }

    private static async Task<string> GetWhenReady(HttpClient client, Process process)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"Testman Web exited with code {process.ExitCode}.");
            }

            try
            {
                return await client.GetStringAsync("/");
            }
            catch (HttpRequestException exception)
            {
                lastException = exception;
                await Task.Delay(100);
            }
        }

        throw new TimeoutException("Testman Web did not become ready.", lastException);
    }

    private static int FindAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private void WriteFile(string relativePath, string content) =>
        File.WriteAllText(Path.Combine(directory, relativePath), content, new UTF8Encoding(false));

    private static string ValidSpecification(string title) => $$"""
        Testman-Format-Version: 1

        # {{title}}

        ## Overview

        Safe overview<script>alert('unsafe')</script>

        ## Preconditions

        Verification precondition

        ## Common steps

        Shared step

        | ID | Major item | Middle item | Minor item | Steps | Expected result |
        |---|---|---|---|---|---|
        | TC-1 | Major | Middle | Minor | Case step | Expected success |
        """;
}
